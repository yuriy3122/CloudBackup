using System.Collections;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Common;
using CloudBackup.Services;

namespace CloudBackup.Management.Controllers
{
    [Route("Core/Restore/Job")]
    public class RestoreController : CommonController
    {
        private const string DefaultOrder = nameof(RestoreJob.StartedAt) + "[desc]";

        private readonly IRepository<RestoreJob> _restoreJobRepository;
        private readonly IRepository<BackupObject> _backupObjectRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly ITenantService _tenantService;

        public RestoreController(IRepository<RestoreJob> restoreJobRepository,
                                 IRepository<BackupObject> backupObjectRepository,
                                 IRoleRepository roleRepository,
                                 IUserRepository userRepository,
                                 ITenantService tenantService) : base(userRepository)
        {
            _restoreJobRepository = restoreJobRepository;
            _backupObjectRepository = backupObjectRepository;
            _roleRepository = roleRepository;
            _tenantService = tenantService;
        }

        [HttpGet]
        public async Task<ActionResult<RestoreJobViewModelList>> GetRestoreJobs(int? pageSize, int? pageNum, 
            string order, string filter)
        {
            var tenantIds = await _tenantService.GetAllowedTenantIds(CurrentUser.Id);

            Expression<Func<RestoreJob, bool>> filterExpression = f => tenantIds.Contains(f.TenantId);

            if (!string.IsNullOrEmpty(filter))
            {
                var filterLower = filter.ToLower();
                filterExpression = filterExpression.And(x => x.Name.ToLower().Contains(filterLower));
            }

            if (!string.IsNullOrEmpty(order))
            {
                order = Regex.Replace(order,
                    nameof(RestoreJobViewModel.ObjectCount),
                    $"{nameof(RestoreJob.RestoreJobObjects)}.{nameof(ICollection.Count)}", RegexOptions.IgnoreCase);
            }

            static IQueryable<RestoreJob> Includes(IQueryable<RestoreJob> i) => 
                i.Include(r => r.RestoreJobObjects).Include(b => b.User);

            var page = pageSize.HasValue && pageNum.HasValue ? new EntitiesPage(pageSize.Value, pageNum.Value) : null;
            var orderBy = string.IsNullOrEmpty(order) ? DefaultOrder : order;

            var restoreJobs = await _restoreJobRepository.FindAsync(filterExpression, orderBy, page, Includes);
            var restoreJobsCount = await _restoreJobRepository.CountAsync(filterExpression, Includes);

            var jobViewModelList = new RestoreJobViewModelList();

            foreach (var restoreJob in restoreJobs)
            {
                var model = GetRestoreJobViewModel(CurrentUser, restoreJob, false);
                jobViewModelList.Items.Add(model);
            }

            jobViewModelList.TotalCount = restoreJobsCount;

            return jobViewModelList;
        }

        [HttpPost]
        public async Task<ActionResult<RestoreJobWizardViewModel>> AddRestoreJob([FromBody] RestoreJobWizardViewModel jobViewModel)
        {
            if (jobViewModel == null)
            {
                throw new ArgumentNullException(nameof(jobViewModel));
            }

            if (jobViewModel.SelectedBackupIds == null)
            {
                throw new ArgumentException("At least one selected backup must be selected.");
            }

            var currentUser = await CheckUserPermissions(Permissions.Write);

            var backupObjects = (await _backupObjectRepository.FindAsync(
                f => jobViewModel.SelectedBackupIds.Contains(f.BackupId),
                null, null, i => i.Include(x => x.Backup).Include(x => x.Profile))).ToList();

            var restoreName = jobViewModel.SelectedBackupIds.Count == 1
                ? backupObjects.Select(x => x.Backup.Name).FirstOrDefault()
                : DateTimeHelper.FormatWithUtcOffset(DateTime.UtcNow);

            var restoreJob = new RestoreJob()
            {
                TenantId = jobViewModel.SelectedTenantId,
                Name = string.Format("Restore Job from {0}", restoreName),
                Status = RestoreJobStatus.Idle,
                RestoreJobObjects = new List<RestoreJobObject>(),
                UserId = CurrentUser.Id,
                RestoreMode = RestoreMode.OriginalLocation
            };

            foreach (var selectedItem in jobViewModel.SelectedItems!)
            {
                var restoreJobObjects = new List<RestoreJobObject>();
                selectedItem.InstanceId = selectedItem.InstanceId != string.Empty ? selectedItem.InstanceId : null;

                if (!string.IsNullOrEmpty(selectedItem.InstanceId))
                {
                    IEnumerable<BackupObject> instanceBackupObjects = backupObjects
                        .Where(b => b.ParentId == selectedItem.InstanceId && b.Type == BackupObjectType.Snapshot)
                        .OrderByDescending(o => o.FinishedAt);

                    var backupObject = instanceBackupObjects.FirstOrDefault();

                    if (backupObject != null)
                    {
                        var volumesToRestore = backupObjects.Where(b => b.BackupId == backupObject.BackupId &&
                                                                        b.ParentId == selectedItem.InstanceId &&
                                                                        b.Type == BackupObjectType.Snapshot);

                        var excludedVolumeIds = (selectedItem.RestoreOptions?.InstanceVolumeRestoreOptions)!
                            .EmptyIfNull()
                            .Where(x => x.Exclude)
                            .Select(x => x.VolumeId)
                            .ToList();

                        if (excludedVolumeIds.Count > 0)
                            volumesToRestore = volumesToRestore.Where(b => !excludedVolumeIds.Contains(b.SourceObjectId));

                        var volumesToRestoreList = volumesToRestore.ToList();
                        var instanceCount = selectedItem.RestoreOptions?.InstanceCount ?? 1;

                        for (int i = 0; i < instanceCount; i++)
                        {
                            var guid = Guid.NewGuid();

                            var volumeRestoreObjects = volumesToRestoreList
                                .Select(x => new RestoreJobObject
                                {
                                    RestoreJob = restoreJob,
                                    ParentId = string.Empty,
                                    BackupObjectId = x.Id,
                                    NewObjectId = string.Empty,
                                    Status = RestoreObjectStatus.Idle,
                                    GroupGuid = guid.ToString()
                                });

                            restoreJobObjects.AddRange(volumeRestoreObjects);
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(selectedItem.DiskId))
                {
                    var diskBackupObjects = backupObjects.Where(b => b.SourceObjectId == selectedItem.DiskId && b.Type == BackupObjectType.Snapshot).OrderByDescending(o => o.FinishedAt);

                    var backupObject = diskBackupObjects.FirstOrDefault();

                    if (backupObject != null)
                    {
                        restoreJobObjects.Add(new RestoreJobObject
                        {
                            RestoreJob = restoreJob,
                            ParentId = string.Empty,
                            BackupObjectId = backupObject.Id,
                            NewObjectId = string.Empty,
                            Status = RestoreObjectStatus.Idle,
                            GroupGuid = Guid.NewGuid().ToString()
                        });
                    }
                }

                var restoreParams = selectedItem.RestoreOptions != null ? JsonConvert.SerializeObject(selectedItem.RestoreOptions) : null;

                foreach (var restoreJobObject in restoreJobObjects)
                {
                    restoreJobObject.RestoreParams = restoreParams ?? string.Empty;
                }

                restoreJob.RestoreJobObjects.AddRange(restoreJobObjects);
            }

            _restoreJobRepository.Add(restoreJob);

            await _restoreJobRepository.SaveChangesAsync();

            var restoreJobViewModel = GetRestoreJobViewModel(currentUser, restoreJob);

            return CreatedAtAction("AddRestoreJob", new { id = restoreJob.Id }, restoreJobViewModel);
        }

        private async Task<User> CheckUserPermissions(Permissions requiredPermissions)
        {
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(CurrentUser.Id);

            if (!permissions.RestoreRights.HasFlag(requiredPermissions))
                throw new UnauthorizedAccessException($"No '{requiredPermissions}' permission for restore jobs.");

            return CurrentUser;
        }

        private static RestoreJobViewModel GetRestoreJobViewModel(User currentUser, RestoreJob restoreJob, 
            bool fillJobObjects = true)
        {
            var offset = currentUser?.UtcOffset ?? new TimeSpan();

            var model = new RestoreJobViewModel
            {
                Id = restoreJob.Id,
                Name = restoreJob.Name,
                UserId = restoreJob.UserId ?? 0,
                Status = restoreJob.Status.ToString(),
                Result = restoreJob.Result?.ToString(),
                RestoreMode = restoreJob.RestoreMode.ToString(),
                RowVersion = Convert.ToBase64String(restoreJob.RowVersion),
                StartedAt = restoreJob.StartedAt.HasValue 
                    ? DateTimeHelper.FormatWithUtcOffset(restoreJob.StartedAt.Value, offset) 
                    : string.Empty,
                FinishedAt = restoreJob.FinishedAt.HasValue 
                    ? DateTimeHelper.FormatWithUtcOffset(restoreJob.FinishedAt.Value, offset) 
                    : string.Empty,
                ObjectCount = restoreJob.RestoreJobObjects.Count
            };

            if (fillJobObjects)
            {
                model.RestoreJobObjectViewModels = restoreJob.RestoreJobObjects
                    .Select(restoreJobObject => new RestoreJobObjectViewModel
                    {
                        RestoreJobId = restoreJob.Id,
                        ParentId = restoreJobObject.ParentId,
                        BackupObjectId = restoreJobObject.BackupObjectId ?? 0,
                        NewObjectId = restoreJobObject.NewObjectId,
                        Status = restoreJobObject.Status.ToString(),
                        StartedAt = restoreJobObject.StartedAt.HasValue
                            ? DateTimeHelper.FormatWithUtcOffset(restoreJobObject.StartedAt.Value, offset)
                            : string.Empty,
                        FinishedAt = restoreJobObject.FinishedAt.HasValue
                            ? DateTimeHelper.FormatWithUtcOffset(restoreJobObject.FinishedAt.Value, offset)
                            : string.Empty
                    })
                    .ToList();
            }

            return model;
        }
    }

    public class RestoreJobViewModelList
    {
        public RestoreJobViewModelList()
        {
            Items = new List<RestoreJobViewModel>();
        }

        public List<RestoreJobViewModel> Items { get; set; }

        public int TotalCount { get; set; }
    }

    public class RestoreJobViewModel
    {
        public int Id { get; set; }//hidden field

        public string? RowVersion { get; set; }//hidden field

        public string? Name { get; set; }

        public int UserId { get; set; }

        public string? Status { get; set; }

        public string? Result { get; set; }

        public string? RestoreMode { get; set; }

        public string? StartedAt { get; set; }

        public string? FinishedAt { get; set; }

        public int ObjectCount { get; set; }

        public List<RestoreJobObjectViewModel>? RestoreJobObjectViewModels { get; set; }
    }

    public class RestoreJobObjectViewModel
    {
        public int RestoreJobId { get; set; }

        public string? ParentId { get; set; }

        public int BackupObjectId { get; set; }

        public string? NewObjectId { get; set; }

        public string? RestoreParams { get; set; }

        public string? Status { get; set; }

        public string? StartedAt { get; set; }

        public string? FinishedAt { get; set; }
    }

    public class RestoreJobItemViewModel
    {
        public RestoreOptions? RestoreOptions { get; set; }

        public string? InstanceId { get; set; }

        public string? DiskId { get; set; }
    }

    public class RestoreJobWizardViewModel
    {
        public int SelectedTenantId { get; set; }

        public List<int>? SelectedBackupIds { get; set; }

        public List<RestoreJobItemViewModel>? SelectedItems { get; set; }
    }
}