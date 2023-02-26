using System.Collections;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Common;
using CloudBackup.Services;

namespace CloudBackup.Management.Controllers
{
    [Route("Core/Backup")]
    public class BackUpController : CommonController
    {
        public const string DefaultOrder = nameof(Backup.StartedAt) + "[desc]";

        private readonly IRepository<Backup> _backupRepository;
        private readonly IRepository<BackupObject> _backupObjectRepository;
        private readonly IBackupService _backupService;
        private readonly IRoleRepository _roleRepository;
        private readonly IProfileService _profileService;
        private readonly ITenantService _tenantService;

        public BackUpController(IRepository<Backup> backupRepository,
                                IRepository<BackupObject> backupObjectRepository,
                                IBackupService backupService,
                                IUserRepository userRepository,
                                IRoleRepository roleRepository,
                                IProfileService profileService,
                                ITenantService tenantService) : base(userRepository)
        {
            _backupRepository = backupRepository;
            _backupObjectRepository = backupObjectRepository;
            _backupService = backupService;
            _roleRepository = roleRepository;
            _profileService = profileService;
            _tenantService = tenantService;
        }

        [HttpGet]
        [Route("{tenantId:int?}")]
        public async Task<ActionResult<ModelList<BackupViewModel>>> GetBackups(
            string backupIds, int? tenantId, [FromQuery] BackupFilter backupFilter,
            int? pageSize, int? pageNum, string order, string filter)
        {
            var currentUser = await CheckUserPermissions(Permissions.Read);

            Expression<Func<Backup, bool>> filterExpression = null!;

            if (!string.IsNullOrEmpty(backupIds))
            {
                var backupIdsList = JsonConvert.DeserializeObject<List<int>>(backupIds);

                if (backupIds.Any())
                {
                    filterExpression = filterExpression.And(x => backupIdsList!.Contains(x.Id));
                }
            }

            if (tenantId != null)
            {
                var allowedTenantIds = await _tenantService.GetAllowedTenantIds(currentUser.Id);
                if (!allowedTenantIds.Contains(tenantId.Value))
                    throw new UnauthorizedAccessException("No permissions to view backups of this tenant.");

                filterExpression = filterExpression.And(f => f.Job.TenantId == tenantId);
            }

            var options = new QueryOptions<Backup>(new EntitiesPage(pageSize ?? short.MaxValue, pageNum ?? 1), 
                FixBackupsOrder(order), filter) { FilterExpression = filterExpression };

            var modelList = await _backupService.GetBackupsAsync(currentUser.Id, backupFilter, options);

            var viewModelList = new ModelList<BackupViewModel>
            {
                Items = modelList.Items.Select(backup => new BackupViewModel(backup, currentUser.UtcOffset)).ToList(),
                TotalCount = modelList.TotalCount
            };

            return viewModelList;
        }

        [HttpPatch]
        public async Task<IActionResult> Patch(int[] ids, [FromBody] BackupViewModel backupViewModel)
        {
            if (ids != null && ids.Length == 0)
            {
                var message = nameof(ids);
                throw new ArgumentException(message);
            }

            await CheckUserPermissions(Permissions.Write);

            var backups = ids != null
                ? (await _backupRepository.FindByIdsAsync(ids, null)).ToList()
                : (await _backupRepository.FindAllAsync(null)).ToList();

            foreach (var backup in backups)
            {
                backup.IsPermanent = backupViewModel.IsPermanent;
                _backupRepository.Update(backup);
            }

            await _backupRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet]
        [Route("Instance")]
        public async Task<ActionResult<ModelList<InstanceViewModel>>> GetInstancesForBackups(
            string backupIds, bool? successStatus, int? pageSize, int? pageNum, string? order)
        {
            var currentUser = await CheckUserPermissions(Permissions.Read);

            var instanceList = new ModelList<InstanceViewModel>();

            if (string.IsNullOrEmpty(backupIds))
            {
                return instanceList;
            }

            var ids = JsonConvert.DeserializeObject<List<int>>(backupIds) ?? new List<int>();

            if (ids.Count == 0)
            {
                return instanceList;
            }

            await _backupService.CheckBackupIdsPermissions(ids ?? new List<int>(), currentUser.Id);

            Expression<Func<BackupObject, bool>> filterExpression = f => 
                ids!.Contains(f.BackupId) && f.Type == BackupObjectType.Snapshot && !string.IsNullOrEmpty(f.ParentId);

            if (successStatus == true)
                filterExpression = filterExpression.And(f => f.Status == BackupObjectStatus.Success || f.Status == BackupObjectStatus.Warning);
            else if (successStatus == false)
                filterExpression = filterExpression.And(f => f.Status == BackupObjectStatus.Failed);

            var backupObjects = (await _backupObjectRepository.FindAsync(filterExpression, null, null, null)).ToArray();

            if (!backupObjects.Any())
            {
                return instanceList;
            }

            var groups = backupObjects.GroupBy(x => new { x.ProfileId });

            foreach (var group in groups)
            {
                var instanceIds = group.Select(x => x.ParentId).Distinct();
                var instanceViewModels = await ProcessInstances(instanceIds, group.Key.ProfileId);
                instanceList.Items.AddRange(instanceViewModels);
            }

            var query = instanceList.Items.DistinctBy(p => p.Id);
            instanceList.Items = query.OrderBy(order ?? InstanceController.DefaultOrder).ToList();
            instanceList.TotalCount = instanceList.Items.Count;

            if (pageSize.HasValue && pageNum.HasValue)
            {
                var skip = (pageNum.Value - 1) * pageSize.Value;
                instanceList.Items = instanceList.Items.Skip(skip).Take(pageSize.Value).ToList();
            }

            return instanceList;
        }

        [HttpGet]
        [Route("Disk")]
        public async Task<ActionResult<ModelList<DiskViewModel>>> GetDisksForBackups(string backupIds, bool? successStatus, int? pageSize, int? pageNum, string? order, bool? showAttached)
        {
            var currentUser = await CheckUserPermissions(Permissions.Read);

            var diskList = new ModelList<DiskViewModel>();

            if (string.IsNullOrEmpty(backupIds))
            {
                return diskList;
            }

            var ids = JsonConvert.DeserializeObject<List<int>>(backupIds) ?? new List<int>();

            if (ids.Count == 0)
            {
                return diskList;
            }

            await _backupService.CheckBackupIdsPermissions(ids ?? new List<int>(), currentUser.Id);

            Expression<Func<BackupObject, bool>> filterExpression = f => ids!.Contains(f.BackupId) && f.Type == BackupObjectType.Snapshot; 
            
            if (showAttached != true)
            {
                filterExpression = filterExpression.And(f => string.IsNullOrEmpty(f.ParentId));
            }

            if (successStatus == true)
                filterExpression = filterExpression.And(f => f.Status == BackupObjectStatus.Success || f.Status == BackupObjectStatus.Warning);
            else if (successStatus == false)
                filterExpression = filterExpression.And(f => f.Status == BackupObjectStatus.Failed);

            var backupObjects = (await _backupObjectRepository.FindAsync(filterExpression, null, null, null)).ToList();

            if (!backupObjects.Any())
            {
                return diskList;
            }

            foreach (var backupObject in backupObjects)
            {
                if (!string.IsNullOrEmpty(backupObject?.BackupParams))
                {
                    var options = JsonConvert.DeserializeObject<BackupOptions>(backupObject.BackupParams);
                    var disk = options?.Disk;

                    if (disk != null)
                    {
                        diskList.Items.Add(new DiskViewModel()
                        {
                            Id = disk.Id,
                            Name = disk.Name,
                            FolderId = disk.FolderId,
                            CreatedAt = disk.CreatedAt,
                            Description = disk.Description,
                            ZoneId = disk.ZoneId,
                            Status = disk.Status,
                            Size = DataSizeHelper.GetFormattedDataSize(long.Parse(disk.Size!)),
                            TypeId = disk.TypeId,
                            ProfileId = backupObject.ProfileId,
                        });
                    }
                }
            }

            var query = diskList.Items.DistinctBy(p => p.Id);
            diskList.Items = query.OrderBy(order ?? DiskController.DefaultOrder).ToList();
            diskList.TotalCount = diskList.Items.Count;

            if (pageSize.HasValue && pageNum.HasValue)
            {
                var skip = (pageNum.Value - 1) * pageSize.Value;
                diskList.Items = diskList.Items.Skip(skip).Take(pageSize.Value).ToList();
            }

            return diskList;
        }

        private async Task<InstanceViewModel[]> ProcessInstances(IEnumerable<string> instanceIds, int profileId)
        {
            var items = new List<InstanceViewModel>();

            var allowedProfileIds = await _profileService.GetAllowedProfileIds(CurrentUser.Id);

            if (!allowedProfileIds.Contains(profileId))
            {
                return items.ToArray();
            }

            foreach (var instanceId in instanceIds)
            {
                var backupObjects = await _backupObjectRepository.FindAsync(
                    f => f.ParentId == instanceId && f.Type == BackupObjectType.Snapshot, null, null, null);

                var backupObject = backupObjects.OrderByDescending(o => o.FinishedAt).FirstOrDefault();

                if (!string.IsNullOrEmpty(backupObject?.BackupParams))
                {
                    var options = JsonConvert.DeserializeObject<BackupOptions>(backupObject.BackupParams);
                    var instance = options?.Instance;

                    if (instance != null)
                    {
                        var instanceViewModel = new InstanceViewModel()
                        {
                            Id = instance.Id,
                            Name = instance.Name,
                            FolderId = instance.FolderId,
                            CreatedAt = instance.CreatedAt,
                            Description = instance.Description,
                            ZoneId = instance.ZoneId,
                            PlatformId = instance.PlatformId,
                            Status = instance.Status,
                            ServiceAccountId = instance.ServiceAccountId,
                            ProfileId = profileId,
                        };

                        items.Add(instanceViewModel);
                    }
                }
            }
            
            return items.ToArray();
        }

        private async Task<User> CheckUserPermissions(Permissions requiredPermissions)
        {
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(CurrentUser.Id);

            if (!permissions.BackupRights.HasFlag(requiredPermissions))
                throw new UnauthorizedAccessException($"No '{requiredPermissions}' permission for backups.");

            return CurrentUser;
        }

        public static string FixBackupsOrder(string order)
        {
            // fix order for properties that are mismatched in viewmodel and model
            if (!string.IsNullOrEmpty(order))
            {
                order = Regex.Replace(order,
                    nameof(BackupViewModel.JobName),
                    $"{nameof(Backup.Job)}.{nameof(Job.Name)}",
                    RegexOptions.IgnoreCase);

                // This replace is needed to avoid local query execution
                order = Regex.Replace(order,
                    nameof(BackupViewModel.ObjectCount),
                    $"{nameof(Backup.BackupObjects)}.{nameof(ICollection.Count)}",
                    RegexOptions.IgnoreCase);
            }
            else
            {
                order = DefaultOrder;
            }

            return order;
        }
    }

    public class BackupViewModel
    {
        public int Id { get; set; }//hidden field
        public string? RowVersion { get; set; }//hidden field
        public string? Name { get; set; }
        public string? JobName { get; set; }
        public bool IsPermanent { get; set; }
        public string? StartedAt { get; set; }
        public string? FinishedAt { get; set; }
        public int ObjectCount { get; set; }
        public string? Status { get; set; }
        public int TenantId { get; set; }
        public bool IsArchive { get; set; }

        public BackupViewModel()
        {
        }

        public BackupViewModel(Backup backup, TimeSpan? utcOffset = null)
        {
            Id = backup.Id;
            RowVersion = Convert.ToBase64String(backup.RowVersion);
            Name = backup.Name;
            JobName = backup.Job?.Name;
            IsPermanent = backup.IsPermanent;
            StartedAt = backup.StartedAt.HasValue
                ? DateTimeHelper.FormatWithUtcOffset(backup.StartedAt.Value, utcOffset ?? TimeSpan.Zero)
                : string.Empty;
            FinishedAt = backup.FinishedAt.HasValue
                ? DateTimeHelper.FormatWithUtcOffset(backup.FinishedAt.Value, utcOffset ?? TimeSpan.Zero)
                : string.Empty;
            ObjectCount = backup.BackupObjects?.Count ?? 0;
            Status = backup.StatusText;
            TenantId = backup.Job?.TenantId ?? 0;
            IsArchive = backup.IsArchive;
        }
    }
}