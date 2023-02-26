using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Common;
using CloudBackup.Services;

namespace CloudBackup.Management.Controllers
{
    [Route("Core/Folder")]
    public class FolderController : CommonController
    {
        private readonly ITenantService _tenantService;
        private readonly IRepository<TenantProfile> _tenantProfileRepository;
        private readonly ICloudClientFactory _cloudClientFactory;

        public FolderController(IUserRepository userRepository,
            IRepository<TenantProfile> tenantProfileRepository,
            ITenantService tenantService,
            ICloudClientFactory cloudClientFactory) : base(userRepository)
        {
            _tenantService = tenantService;
            _tenantProfileRepository = tenantProfileRepository;
            _cloudClientFactory = cloudClientFactory;
        }

        [HttpGet]
        public async Task<ActionResult<ModelList<CloudFolder>>> GetFolders(string cloudId)
        {
            int[] tenantIds = await _tenantService.GetAllowedTenantIds(CurrentUser.Id);

            static IQueryable<TenantProfile> Includes(IQueryable<TenantProfile> i) => i.Include(p => p.Profile);
            var profileEntities = await _tenantProfileRepository.FindAsync(f => tenantIds.ToList().Contains(f.TenantId), string.Empty, null, Includes);

            var clouds = new List<CloudFolder>();

            foreach (var profileEntity in profileEntities)
            {
                var credentials = new CloudCredentials
                {
                    ServiceAccountId = profileEntity.Profile.ServiceAccountId,
                    PrivateKey = profileEntity.Profile.PrivateKey,
                    KeyId = profileEntity.Profile.KeyId
                };

                var cloudClient = _cloudClientFactory.CreateCloudClient(credentials);

                var result = await cloudClient.GetCloudList();

                var cloud = result?.Clouds?.FirstOrDefault(x => x.Id == cloudId);

                if (cloud != null)
                {
                    var folderList = await cloudClient.GetCloudFolderList(cloudId);

                    if (folderList != null && folderList.Folders?.Count > 0)
                    {
                        clouds.AddRange(folderList.Folders);
                    }
                }
            }

            return new ModelList<CloudFolder>(clouds);
        }
    }
}