using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Services;

namespace CloudBackup.Management.Controllers
{
    [Route("Administration/Tenant")]
    public class TenantController : CommonController
    {
        private readonly ITenantService _tenantService;
        private readonly IUserService _userService;
        private readonly IRoleRepository _roleRepository;
        private readonly IRepository<TenantProfile> _tenantProfileRepository;

        public TenantController(ITenantService tenantService, 
                                IUserService userService,
                                IUserRepository userRepository,
                                IRoleRepository roleRepository,
                                IRepository<TenantProfile> tenantProfileRepository) : base(userRepository)
        {
            _tenantService = tenantService;
            _userService = userService;
            _roleRepository = roleRepository;
            _tenantProfileRepository = tenantProfileRepository;
        }

        [HttpGet]
        public async Task<ActionResult<ModelList<TenantViewModel>>> GetTenants(int? pageSize, int? pageNum, string order, string filter)
        {
            var modelList = await _tenantService.GetTenantsAsync(CurrentUser.Id, 
                new QueryOptions<Tenant>(new EntitiesPage(pageSize ?? short.MaxValue, pageNum ?? 1), order, filter));

            var viewModelList = await GetViewModelList(modelList, CurrentUser);

            return viewModelList;
        }

        [HttpGet]
        [Route("{tenantId:int}")]
        public async Task<ActionResult<TenantViewModel>> GetTenant(int tenantId)
        {
            var tenant = await _tenantService.GetTenantAsync(CurrentUser.Id, tenantId);
            var tenantViewModel = new TenantViewModel(tenant);

            await ProcessViewModels(CurrentUser.Id, new[] {tenantViewModel});

            return tenantViewModel;
        }

        [HttpGet]
        [Route("Allowed")]
        public async Task<ActionResult<ModelList<TenantViewModel>>> GetAllowedTenants(int? pageSize, int? pageNum, string order, string filter)
        {
            var allowedTenantIds = await _tenantService.GetAllowedTenantIds(CurrentUser.Id);

            var queryOptions = new QueryOptions<Tenant>(
                new EntitiesPage(pageSize ?? short.MaxValue, pageNum ?? 1), 
                order, filter, tenant => allowedTenantIds.Contains(tenant.Id));

            var modelList = await _tenantService.GetTenantsAsync(CurrentUser.Id, queryOptions);

            var viewModelList = await GetViewModelList(modelList, CurrentUser);

            return viewModelList;
        }

        [HttpGet]
        [Route("{tenantId:int}/Users")]
        public async Task<ActionResult<ModelList<User>>> GetUsersByTenant(int tenantId, int? pageSize, int? pageNum, string order, string filter)
        {
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(CurrentUser.Id);

            if (!permissions.UserRights.HasFlag(Permissions.Read))
                throw new UnauthorizedAccessException("No rights to view users.");

            if (CurrentUser.TenantId != tenantId && !permissions.IsGlobalAdmin)
                throw new UnauthorizedAccessException("No permissions to view other tenant's data.");

            var result = await _userService.GetUsersAsync(CurrentUser.Id, 
                new QueryOptions<User>(new EntitiesPage(pageSize ?? short.MaxValue, pageNum ?? 1), order, filter, user => user.TenantId == tenantId));

            return result;
        }

        [HttpGet]
        [Route("{tenantId:int}/Profiles")]
        public async Task<ActionResult<ModelList<ProfileViewModel>>> GetProfilesByTenant(int tenantId)
        {
            var tenantIds = await _tenantService.GetAllowedTenantIds(CurrentUser.Id);

            var viewModelList = new ModelList<ProfileViewModel>();

            if (!tenantIds.Contains(tenantId))
            {
                return viewModelList;
            }

            var tenantProfiles = await _tenantProfileRepository.FindAsync(f => f.TenantId == tenantId, null, null, i => i.Include(p => p.Profile));

            var profiles = tenantProfiles.Select(x => x.Profile).ToList();

            viewModelList.TotalCount = profiles.Count;

            foreach (var profile in profiles)
            {
                viewModelList.Items.Add(new ProfileViewModel(profile));
            }

            return viewModelList;
        }

        [HttpPost]
        public async Task<ActionResult<TenantViewModel>> AddTenant([FromBody] TenantViewModel tenantViewModel)
        {
            if (tenantViewModel == null)
                throw new ArgumentNullException(nameof(tenantViewModel));

            var tenant = tenantViewModel.GetTenant();

            var newTenant = await _tenantService.AddTenantAsync(CurrentUser.Id, tenant);
            var newTenantViewModel = new TenantViewModel(newTenant);

            return CreatedAtAction(nameof(GetTenant), new {tenantId = newTenant.Id}, newTenantViewModel);
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> UpdateTenant(int id, [FromBody] TenantViewModel tenantViewModel)
        {
            if (tenantViewModel == null)
                throw new ArgumentNullException(nameof(tenantViewModel));

            var tenant = tenantViewModel.GetTenant();

            await _tenantService.UpdateTenantAsync(CurrentUser.Id, id, tenant);

            return NoContent();
        }

        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IActionResult> RemoveTenant(int id)
        {
            await _tenantService.RemoveTenantAsync(CurrentUser.Id, id);

            return NoContent();
        }

        private async Task<ModelList<TenantViewModel>> GetViewModelList(ModelList<Tenant> modelList, User currentUser)
        {
            var tenantViewModels = modelList.Items.Select(tenant => new TenantViewModel(tenant)).ToList();

            await ProcessViewModels(currentUser.Id, tenantViewModels);
            
            var viewModelList = ModelList.Create(tenantViewModels, modelList.TotalCount);
            return viewModelList;
        }

        private async Task ProcessViewModels(int currentUserId, IReadOnlyCollection<TenantViewModel> tenantViewModels)
        {
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(currentUserId);

            // check for jobs existence
            var tenantIdsWithJobs = await _tenantService.GetTenantIdsWithJobs();
            
            foreach (var tenantViewModel in tenantViewModels)
            {
                if (tenantIdsWithJobs.Contains(tenantViewModel.Id) && tenantViewModel.CanDelete)
                {
                    tenantViewModel.CanDelete = false;
                    tenantViewModel.CantDeleteReason = "Can't delete tenant with jobs.";
                }
            }

            // set admins
            if (permissions.UserRights.HasFlag(Permissions.Read))
            {
                var admins = await _userService.GetUsersAsync(currentUserId,
                    new QueryOptions<User>
                    {
                        FilterExpression = user =>
                            user.UserRoles.Select(x => x.Role).Any(role => role != null && (role.IsGlobalAdmin || role.IsUserAdmin))
                    });
                var tenantIdToAdmins = admins.Items.ToLookup(x => x.TenantId);

                foreach (var tenantViewModel in tenantViewModels)
                {
                    tenantViewModel.Admins = tenantIdToAdmins[tenantViewModel.Id]
                        .Select(user => new UserViewModel
                        {
                            Id = user.Id,
                            Name = user.Name,
                            Description = user.Description,
                            Login = user.Login
                        })
                        .ToList();
                }
            }
        }
    }

    public class TenantViewModel
    {
        public int Id { get; set; } // hidden field
        public string? RowVersion { get; set; }//hidden field
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<UserViewModel>? Admins { get; set; }

        public bool Isolated { get; set; }
        public bool IsSystem { get; set; }

        public bool CanEdit { get; set; } = true;
        public string? CantEditReason { get; set; }
        public bool CanDelete { get; set; } = true;
        public string? CantDeleteReason { get; set; }

        public TenantViewModel() { }

        public TenantViewModel(Tenant tenant)
        {
            if (tenant == null)
                throw new ArgumentNullException(nameof(tenant));

            Id = tenant.Id;
            RowVersion = tenant.RowVersion == null ? null : Convert.ToBase64String(tenant.RowVersion);
            Name = tenant.Name;
            Description = tenant.Description;
            Isolated = tenant.Isolated;
            IsSystem = tenant.IsSystem;

            if (!tenant.IsSystem)
            {
                CanEdit = !tenant.IsSystem;
                CanDelete = !tenant.IsSystem;
                CantEditReason = "Can't edit system tenant.";
                CantDeleteReason = "Can't delete system tenant.";
            }
        }

        public Tenant GetTenant()
        {
            return new Tenant
            {
                Id = Id,
                RowVersion = Convert.FromBase64String(RowVersion ?? string.Empty),
                Name = Name ?? string.Empty,
                Description = Description ?? string.Empty,
                Isolated = Isolated,
                IsSystem = IsSystem
            };
        }
    }
}