using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Common;
using CloudBackup.Services;

namespace CloudBackup.Management.Controllers
{
    [Route("Core/Cloud")]
    public class CloudController : CommonController
    {
        private readonly ITenantService _tenantService;
        private readonly IRepository<TenantProfile> _tenantProfileRepository;
        private readonly ICloudClientFactory _cloudClientFactory;

        public CloudController(IUserRepository userRepository,
                               IRepository<TenantProfile> tenantProfileRepository,
                               ITenantService tenantService,
                               ICloudClientFactory cloudClientFactory) : base(userRepository)
        {
            _tenantService = tenantService;
            _tenantProfileRepository = tenantProfileRepository;
            _cloudClientFactory = cloudClientFactory;
        }

        [HttpGet]
        public async Task<ActionResult<ModelList<Cloud>>> GetClouds(int? profileId)
        {
            int[] tenantIds = await _tenantService.GetAllowedTenantIds(CurrentUser.Id);

            static IQueryable<TenantProfile> Includes(IQueryable<TenantProfile> i) => i.Include(p => p.Profile);

            Expression<Func<TenantProfile, bool>> filter = f => tenantIds.ToList().Contains(f.TenantId);

            if (profileId != null)
            {
                filter = filter.And(f => f.ProfileId == profileId.Value);
            }

            var profileEntities = await _tenantProfileRepository.FindAsync(filter, string.Empty, null, Includes);

            var clouds = new List<Cloud>();

            foreach (var profileEntity in profileEntities)
            {
                var credentials = new CloudCredentials
                {
                    ServiceAccountId = profileEntity.Profile.ServiceAccountId,
                    KeyId = profileEntity.Profile.KeyId,
                    PrivateKey = profileEntity.Profile.PrivateKey
                };

                var cloudClient = _cloudClientFactory.CreateCloudClient(credentials);

                var result = await cloudClient.GetCloudList();

                if (result != null && result.Clouds != null)
                {
                    clouds.AddRange(result.Clouds);
                }
            }

            return new ModelList<Cloud>(clouds);
        }
    }
}