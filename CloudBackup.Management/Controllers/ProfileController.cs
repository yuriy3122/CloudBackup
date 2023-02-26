using Microsoft.AspNetCore.Mvc;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Common;
using CloudBackup.Services;
using System.Linq.Expressions;
using CloudBackup.Common.Exceptions;

namespace CloudBackup.Management.Controllers
{
    [Route("Administration/Profile")]
    public class ProfileController : CommonController
    {
        private readonly IProfileService _profileService;
        private readonly IUserRepository _userRepository;

        public ProfileController(IProfileService profileService, IUserRepository userRepository) : base(userRepository)
        {
            _profileService = profileService;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<ModelList<ProfileViewModel>>> GetProfiles(int? tenantId, int? pageSize, int? pageNum, string order, string filter)
        {
            Expression<Func<Profile, bool>>? filterExpression = null;

            if (tenantId != null)
                filterExpression = x => x.TenantProfiles!.Any(y => y.TenantId == tenantId);

            var page = new EntitiesPage(pageSize ?? short.MaxValue, pageNum ?? 1);

            var options = filterExpression != null ? new QueryOptions<Profile>(page, order, filter, filterExpression) :
                                                     new QueryOptions<Profile>(page, order, filter);

            var modelList = await _profileService.GetProfilesAsync(CurrentUser.Id, options);

            var viewModelList = await GetViewModelList(modelList);

            return viewModelList;
        }

        [HttpGet]
        [Route("{profileId:int}")]
        public async Task<ActionResult<ProfileViewModel>> GetProfile(int profileId)
        {
            var options = new QueryOptions<Profile> {FilterExpression = x => x.Id == profileId};

            var modelList = await _profileService.GetProfilesAsync(CurrentUser.Id, options);
            var viewModelList = await GetViewModelList(modelList);

            var profileViewModel = viewModelList.Items.SingleOrDefault();
            if (profileViewModel == null)
                throw new ObjectNotFoundException("There is no profile with this id.");

            return profileViewModel;
        }

        [HttpPost]
        public async Task<ActionResult<ProfileViewModel>> AddProfile([FromBody] ProfileViewModel profileViewModel)
        {
            if (profileViewModel == null)
                throw new ArgumentNullException(nameof(profileViewModel));

            var profile = profileViewModel.GetProfile();

            var newProfile = await _profileService.AddProfileAsync(CurrentUser.Id, profile);
            var newProfileViewModel = new ProfileViewModel(newProfile);

            return CreatedAtAction(nameof(GetProfile), new {profileId = newProfile.Id}, newProfileViewModel);
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] ProfileViewModel profileViewModel)
        {
            if (profileViewModel == null)
                throw new ArgumentNullException(nameof(profileViewModel));

            var profile = profileViewModel.GetProfile();

            await _profileService.UpdateProfileAsync(CurrentUser.Id, id, profile);

            return NoContent();
        }

        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IActionResult> RemoveProfile(int id)
        {
            await _profileService.RemoveProfileAsync(CurrentUser.Id, id);

            return NoContent();
        }

        private async Task<ModelList<ProfileViewModel>> GetViewModelList(ModelList<Profile> modelList)
        {
            var profileIdsWithJobs = await _profileService.GetProfileIdsWithJobs();

            var profileViewModels = modelList.Items
                .Select(profile => new ProfileViewModel(profile) { UsedInJobs = profileIdsWithJobs.Contains(profile.Id) })
                .ToList();
            var viewModelList = new ModelList<ProfileViewModel>(profileViewModels, modelList.TotalCount);
            return viewModelList;
        }
    }

    public class ProfileViewModel
    {
        public int Id { get; set; } // hidden field
        public string? RowVersion { get; set; }//hidden field
        public string? Name { get; set; }
        public string? Description { get; set; }

        public string? ServiceAccountId { get; set; }
        public string? KeyId { get; set; }
        public string? PrivateKey { get; set; }

        public UserViewModel? Owner { get; set; }

        public DateTime Created { get; set; }
        public string? CreatedText => DateTimeHelper.Format(Created);

        public List<TenantViewModel>? Tenants { get; set; }
        public TenantViewModel Tenant
        {
            get => Tenants?.FirstOrDefault() ?? new TenantViewModel();
            set => Tenants = value != null ? new List<TenantViewModel> { value } : null;
        }

        public bool UsedInJobs { get; set; }

        public ProfileViewModel()
        {
        }

        public ProfileViewModel(Profile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            Id = profile.Id;
            RowVersion = profile.RowVersion == null ? null : Convert.ToBase64String(profile.RowVersion);
            Name = profile.Name;
            Description = profile.Description;
            ServiceAccountId = profile.ServiceAccountId;
            KeyId = profile.KeyId;
            PrivateKey = profile.PrivateKey;
            Created = profile.Created;
            Owner = profile.Owner != null
                ? new UserViewModel(profile.Owner)
                : (profile?.OwnerUserId != null ? new UserViewModel {Id = profile.OwnerUserId} : null);

            Tenants = profile?.TenantProfiles?
                .Select(x => x.Tenant != null ? new TenantViewModel(x.Tenant) : new TenantViewModel {Id = x.TenantId})
                .EmptyIfNull()
                .ToList();
        }

        public Profile GetProfile()
        {
            var profile = new Profile
            {
                Id = Id,
                AuthenticationType = AuthenticationType.IAMUser,
                RowVersion = Convert.FromBase64String(RowVersion ?? string.Empty),
                Name = Name,
                Description = Description,
                ServiceAccountId = ServiceAccountId,
                KeyId = KeyId,
                PrivateKey = PrivateKey,
                Created = Created,
                OwnerUserId = Owner?.Id ?? 0,
                Owner = Owner?.GetUser()
            };

            profile.TenantProfiles = Tenants?.Select(tenant => new TenantProfile
            {
                ProfileId = profile.Id,
                Profile = profile,
                TenantId = tenant.Id,
                Tenant = tenant.GetTenant()
            }).ToList();

            return profile;
        }
    }
}