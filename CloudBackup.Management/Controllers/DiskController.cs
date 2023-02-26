using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Common;
using CloudBackup.Services;

namespace CloudBackup.Management.Controllers
{
    [Route("Core/Disk")]
    public class DiskController : CommonController
    {
        public const string DefaultOrder = nameof(DiskViewModel.Name);

        private readonly ITenantService _tenantService;
        private readonly IRepository<TenantProfile> _tenantProfileRepository;
        private readonly ICloudClientFactory _cloudClientFactory;
        private readonly IComputeCloudClientFactory _computeCloudClientFactory;

        public DiskController(IUserRepository userRepository,
            IRepository<TenantProfile> tenantProfileRepository,
            ITenantService tenantService,
            ICloudClientFactory cloudClientFactory,
            IComputeCloudClientFactory computeCloudClientFactory) : base(userRepository)
        {
            _tenantService = tenantService;
            _tenantProfileRepository = tenantProfileRepository;
            _cloudClientFactory = cloudClientFactory;
            _computeCloudClientFactory = computeCloudClientFactory;
        }

        [HttpGet]
        public async Task<ActionResult<ModelList<DiskViewModel>>> GetDisks(string folderId, string? diskIds, int? pageSize, int? pageNum, string? order)
        {
            int[] tenantIds = await _tenantService.GetAllowedTenantIds(CurrentUser.Id);
            var diskIdList = JsonConvert.DeserializeObject<List<string>>(diskIds ?? "") ?? new List<string>();

            static IQueryable<TenantProfile> Includes(IQueryable<TenantProfile> i) => i.Include(p => p.Profile);
            var profileEntities = await _tenantProfileRepository.FindAsync(f => tenantIds.ToList().Contains(f.TenantId), string.Empty, null, Includes);

            var clouds = new List<CloudFolder>();
            var result = new ModelList<DiskViewModel>();

            foreach (var profileEntity in profileEntities)
            {
                var credentials = new CloudCredentials
                {
                    ServiceAccountId = profileEntity.Profile.ServiceAccountId,
                    PrivateKey = profileEntity.Profile.PrivateKey,
                    KeyId = profileEntity.Profile.KeyId
                };

                var cloudClient = _cloudClientFactory.CreateCloudClient(credentials);

                var cloudList = await cloudClient.GetCloudList();

                if (cloudList == null || cloudList.Clouds == null) continue;

                foreach (var cloud in cloudList.Clouds)
                {
                    var folderList = await cloudClient.GetCloudFolderList(cloud.Id ?? string.Empty);

                    if (folderList == null || folderList.Folders == null) continue;

                    if (folderList.Folders.Any(x => x.Id == folderId))
                    {
                        var computeCloudClient = _computeCloudClientFactory.CreateComputeCloudClient(credentials);

                        var diskList = await computeCloudClient.GetDiskList(folderId);

                        if (diskList == null || diskList.Disks == null) continue;

                        if (diskIdList.Any())
                        {
                            foreach (var diskId in diskIdList)
                            {
                                var disk = diskList.Disks.FirstOrDefault(x => x.Id == diskId);

                                if (disk == null)
                                {
                                    result?.Items.Add(new DiskViewModel() { Id = diskId, Status = "DELETED" });
                                }
                                else
                                {
                                    result?.Items.Add(new DiskViewModel(disk, profileEntity.ProfileId));
                                }
                            }
                        }
                        else
                        {
                            foreach (var disk in diskList.Disks)
                            {
                                result?.Items.Add(new DiskViewModel(disk, profileEntity.ProfileId));
                            }
                        }
                    }
                }
            }

            result.TotalCount = result.Items.Count;

            if (!string.IsNullOrEmpty(order))
            {
                result.Items = result.Items.OrderBy(order ?? DefaultOrder).ToList();
            }

            if (pageSize.HasValue && pageNum.HasValue)
            {
                var skip = (pageNum.Value - 1) * pageSize.Value;
                result.Items = result.Items.Skip(skip).Take(pageSize.Value).ToList();
            }

            return result;
        }
    }

    public class DiskViewModel
    {
        public DiskViewModel()
        {
        }

        public DiskViewModel(DiskDescription diskDescription, int profileId)
        {
            Id = diskDescription.Id;
            FolderId = diskDescription.FolderId;
            CreatedAt = diskDescription.CreatedAt;
            Name = diskDescription.Name;
            Description = diskDescription.Description;
            TypeId = diskDescription.TypeId;
            ZoneId = diskDescription.ZoneId;
            Status = diskDescription.Status;
            ProfileId = profileId;

            if (long.TryParse(diskDescription.Size, out long size))
            {
                Size = DataSizeHelper.GetFormattedDataSize(size);
            }
        }

        public string? Id { get; set; }
        public string? FolderId { get; set; }
        public string? CreatedAt { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? TypeId { get; set; }
        public string? ZoneId { get; set; }
        public string? Size { get; set; }
        public string? Status { get; set; }
        public int? ProfileId { get; set; }
    }
}