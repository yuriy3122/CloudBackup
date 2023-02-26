using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Common;
using CloudBackup.Services;

namespace CloudBackup.Management.Controllers
{
    [Route("Core/Instance")]
    public class InstanceController : CommonController
    {
        public const string DefaultOrder = nameof(InstanceViewModel.Name);

        private readonly ITenantService _tenantService;
        private readonly IRepository<TenantProfile> _tenantProfileRepository;
        private readonly ICloudClientFactory _cloudClientFactory;
        private readonly IComputeCloudClientFactory _computeCloudClientFactory;

        public InstanceController(IUserRepository userRepository,
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
        public async Task<ActionResult<ModelList<InstanceViewModel>>> GetInstances(string folderId, string? instanceIds, int? pageSize, int? pageNum, string? order, string? filter)
        {
            int[] tenantIds = await _tenantService.GetAllowedTenantIds(CurrentUser.Id);
            var instanceIdsList = JsonConvert.DeserializeObject<List<string>>(instanceIds ?? "") ?? new List<string>();

            static IQueryable<TenantProfile> Includes(IQueryable<TenantProfile> i) => i.Include(p => p.Profile);
            var profileEntities = await _tenantProfileRepository.FindAsync(f => tenantIds.ToList().Contains(f.TenantId), string.Empty, null, Includes);

            var clouds = new List<CloudFolder>();
            var result = new ModelList<InstanceViewModel>();

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

                        var instanceList = await computeCloudClient.GetInstanceList(folderId);

                        if (instanceList == null || instanceList.Instances == null) continue;

                        if (instanceIdsList.Any())
                        {
                            foreach (var instanceId in instanceIdsList)
                            {
                                var instance = instanceList.Instances.FirstOrDefault(x => x.Id == instanceId);

                                if (instance == null)
                                {
                                    result?.Items.Add(new InstanceViewModel() { Id = instanceId, Status = "DELETED" });
                                }
                                else
                                {
                                    result?.Items.Add(new InstanceViewModel(instance, profileEntity.ProfileId));
                                }
                            }
                        }
                        else
                        {
                            foreach (var instance in instanceList.Instances)
                            {
                                if (instance.Id == InstanceMetadata.GetInstanceId())
                                {
                                    continue;
                                }

                                if (!string.IsNullOrEmpty(filter))
                                {
                                    var name = instance.Name?.ToLowerInvariant() ?? string.Empty;

                                    if (!name.Contains(filter))
                                    {
                                        continue;
                                    }
                                }

                                result?.Items.Add(new InstanceViewModel(instance, profileEntity.ProfileId));
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

        [HttpGet]
        [Route("InstanceStates")]
        public ActionResult<List<string>> GetInstanceStates()
        {
            var instanceStates = new List<string>()
                { 
                    "PROVISIONING",
                    "RUNNING",
                    "STOPPING",
                    "STOPPED",
                    "STARTING",
                    "RESTARTING",
                    "UPDATING",
                    "ERROR",
                    "CRASHED",
                    "DELETING"
                };

            return instanceStates;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("Running")]
        public async Task<IActionResult> StartInstances([FromBody] string[] instanceIds)
        {
            await ExecuteOperation(instanceIds, InstanceOperation.Start);

            return Ok();
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("Stopped")]
        public async Task<IActionResult> StopInstances([FromBody] string[] instanceIds)
        {
            await ExecuteOperation(instanceIds, InstanceOperation.Stop);

            return Ok();
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("Rebooting")]
        public async Task<IActionResult> RebootInstances([FromBody] string[] instanceIds)
        {
            await ExecuteOperation(instanceIds, InstanceOperation.Reboot);

            return Ok();
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("Terminated")]
        public async Task<IActionResult> TerminateInstances([FromBody] string[] instanceIds)
        {
            await ExecuteOperation(instanceIds, InstanceOperation.Terminate);

            return Ok();
        }

        private async Task ExecuteOperation(string[] instanceIds, InstanceOperation operation)
        {
            if (instanceIds == null || instanceIds.Length == 0)
            {
                var errorMessage = "instances not defined";
                throw new ArgumentException(errorMessage);
            }

            int[] tenantIds = await _tenantService.GetAllowedTenantIds(CurrentUser.Id);

            static IQueryable<TenantProfile> Includes(IQueryable<TenantProfile> i) => i.Include(p => p.Profile);
            var profileEntities = await _tenantProfileRepository.FindAsync(f => tenantIds.ToList().Contains(f.TenantId), string.Empty, null, Includes);

            var clouds = new List<CloudFolder>();
            var result = new ModelList<InstanceViewModel>();

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

                    foreach (var folder in folderList.Folders)
                    {
                        var computeCloudClient = _computeCloudClientFactory.CreateComputeCloudClient(credentials);

                        var instanceList = await computeCloudClient.GetInstanceList(folder.Id!);

                        if (instanceList == null || instanceList.Instances == null) continue;

                        var items = instanceList.Instances.Where(x => instanceIds.Contains(x.Id)).Select(x => x.Id);
                        var iids = items?.ToArray() as string[];

                        if (iids != null)
                        {
                            switch (operation)
                            {
                                case InstanceOperation.Start:
                                    await computeCloudClient.StartInstances(folder.Id!, iids);
                                    break;
                                case InstanceOperation.Stop:
                                    await computeCloudClient.StopInstances(folder.Id!, iids);
                                    break;
                                case InstanceOperation.Reboot:
                                    await computeCloudClient.RestartInstances(folder.Id!, iids);
                                    break;
                                case InstanceOperation.Terminate:
                                    await computeCloudClient.DeleteInstances(iids);
                                    break;
                                default:
                                    break;
                            }            
                        }
                    }
                }
            }
        }
    }

    public class InstanceViewModel
    {
        public InstanceViewModel()
        {
        }

        public InstanceViewModel(Instance instance, int profileId)
        {
            Id = instance.Id;
            Name = instance.Name;
            FolderId = instance.FolderId;
            CreatedAt = instance.CreatedAt;
            Description = instance.Description;
            ZoneId = instance.ZoneId;
            PlatformId = instance.PlatformId;
            Status = instance.Status;
            ServiceAccountId = instance.ServiceAccountId;
            ProfileId = profileId;
        }

        public string? Id { get; set; }
        public string? FolderId { get; set; }
        public string? CreatedAt { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ZoneId { get; set; }
        public string? PlatformId { get; set; }
        public string? Status { get; set; }
        public string? ServiceAccountId { get; set; }
        public int? ProfileId { get; set; }
    }

    public enum InstanceOperation
    {
        Start = 0,
        Stop = 1,
        Reboot = 2,
        Terminate = 3
    }
}