using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Linq.Expressions;
using CloudBackup.Common;
using CloudBackup.Common.Exceptions;
using CloudBackup.Model;
using CloudBackup.Repositories;

namespace CloudBackup.Services
{
    public interface ITenantService
    {
        Task<ModelList<Tenant>> GetTenantsAsync(int userId, QueryOptions<Tenant>? options = null);

        Task<Tenant> GetTenantAsync(int userId, int tenantId);

        Task<Tenant> AddTenantAsync(int userId, Tenant tenant);

        Task UpdateTenantAsync(int userId, int tenantId, Tenant tenant);

        Task RemoveTenantAsync(int userId, int tenantId);

        /// <summary>
        /// Gets tenant ids which data can be accessed by a specified user.
        /// </summary>
        /// <param name="userId">ID of a user which tenant's ids need to be returned.</param>
        Task<int[]> GetAllowedTenantIds(int userId);

        Task<ImmutableHashSet<int>> GetTenantIdsWithJobs();
    }

    public class TenantService : ITenantService
    {
        private const string DefaultOrder = $"{nameof(Tenant.IsSystem)}[desc]";

        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRepository<Profile> _profileRepository;
        private readonly IRepository<Job> _jobRepository;
        private readonly IRepository<Schedule> _scheduleRepository;

        public TenantService(
            IRepository<Tenant> tenantRepository,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRepository<Profile> profileRepository,
            IRepository<Job> jobRepository,
            IRepository<Schedule> scheduleRepository)
        {
            _tenantRepository = tenantRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _profileRepository = profileRepository;
            _jobRepository = jobRepository;
            _scheduleRepository = scheduleRepository;
        }

        public async Task<ModelList<Tenant>> GetTenantsAsync(int userId, QueryOptions<Tenant>? options = null)
        {
            var currentUser = await _userRepository.FindByIdAsync(userId, null);
            if (currentUser == null)
                throw new ObjectNotFoundException("Current user not found.");

            // applying rights
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(userId);
            if (!permissions.TenantRights.HasFlag(Permissions.Read))
                throw new UnauthorizedAccessException("No rights to view tenants.");

            Expression<Func<Tenant, bool>> filterExpression = null!;

            if (options != null)
            {
                filterExpression = options.FilterExpression;
            }

            if (!permissions.IsGlobalAdmin)
                filterExpression = filterExpression.And(x => x.Id == currentUser.TenantId);

            if (!string.IsNullOrEmpty(options?.Filter))
            {
                var filterLower = options.Filter.ToLower();
                filterExpression = f => f.Name.ToLower().Contains(filterLower)
                                        || f.Description.ToLower().Contains(filterLower);
            }

            var tenants = await _tenantRepository.FindAsync(filterExpression, options?.OrderBy ?? DefaultOrder, options?.Page, null);

            var tenantsCount = await _tenantRepository.CountAsync(filterExpression, null);

            var tenantsList = ModelList.Create(tenants, tenantsCount);

            return tenantsList;
        }

        public async Task<Tenant> GetTenantAsync(int userId, int tenantId)
        {
            // applying rights
            var user = await _userRepository.FindByIdAsync(userId, null);
            if (user == null)
                throw new ObjectNotFoundException("User not found.");

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(user.Id);
            if (!permissions.TenantRights.HasFlag(Permissions.Read))
                throw new UnauthorizedAccessException("No rights to view tenants.");

            if (!permissions.IsGlobalAdmin && tenantId != user.TenantId)
                throw new UnauthorizedAccessException("No access to this tenant.");


            var tenant = await _tenantRepository.FindByIdAsync(tenantId, p => p.Include(x => x.Users));
            if (tenant == null)
                throw new ObjectNotFoundException("Tenant not found.");

            return tenant;
        }

        public async Task<Tenant> AddTenantAsync(int userId, Tenant tenant)
        {
            if (tenant == null)
            {
                throw new ArgumentNullException(nameof(tenant));
            }

            // checking rights
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(userId);
            if (!permissions.TenantRights.HasFlag(Permissions.Write))
                throw new UnauthorizedAccessException("No rights to edit tenants.");

            ValidateTenant(tenant);

            var tenantToAdd = new Tenant
            {
                Name = tenant.Name,
                Description = tenant.Description,
                Isolated = tenant.Isolated
            };

            if (tenant.TenantProfiles != null)
            {
                tenantToAdd.TenantProfiles = tenant.TenantProfiles.Select(x => new TenantProfile { TenantId = x.TenantId, ProfileId = x.ProfileId }).ToList();
            }

            _tenantRepository.Add(tenantToAdd);

            await _tenantRepository.SaveChangesAsync();

            return tenantToAdd;
        }

        public async Task UpdateTenantAsync(int userId, int tenantId, Tenant tenant)
        {
            if (tenant == null)
                throw new ArgumentNullException(nameof(tenant));

            // checking rights
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(userId);
            if (!permissions.TenantRights.HasFlag(Permissions.Write))
                throw new UnauthorizedAccessException("No rights to edit tenants.");

            ValidateTenant(tenant);

            var tenantToUpdate = await _tenantRepository.FindByIdAsync(tenantId, null);
            if (tenantToUpdate == null)
                throw new ObjectNotFoundException("There is no tenant with this id.");
            if (tenantToUpdate.IsSystem)
                throw new NotSupportedException("It is forbidden to edit a system tenant.");

            tenantToUpdate.Name = tenant.Name;
            tenantToUpdate.Description = tenant.Description;
            tenantToUpdate.Isolated = tenant.Isolated;

            await _tenantRepository.SaveChangesAsync();
        }

        public async Task RemoveTenantAsync(int userId, int tenantId)
        {
            // checking rights
            var currentUser = await _userRepository.FindByIdAsync(userId, null);
            if (currentUser == null)
                throw new ObjectNotFoundException("User not found.");

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(userId);
            if (!permissions.TenantRights.HasFlag(Permissions.Write))
                throw new UnauthorizedAccessException("No rights to edit tenants.");

            if (tenantId == currentUser.TenantId)
                throw new NotSupportedException("It is forbidden to delete a tenant which member you are of.");

            var tenant = await _tenantRepository.FindByIdAsync(tenantId, null);

            if (tenant == null)
                throw new ObjectNotFoundException("There is no tenant with this id.");
            if (tenant.IsSystem)
                throw new NotSupportedException("It is forbidden to delete a system tenant.");

            var tenantIdsWithJobs = await GetTenantIdsWithJobs();

            if (tenantIdsWithJobs.Contains(tenantId))
                throw new NotSupportedException("Can't delete a tenant who has any jobs.");

            // Delete profiles associated with tenant
            // TODO remove if profiles will stop supporting multiple tenants (because then it will be 1-1 relationship which will cascade)
            var profiles = await _profileRepository.FindAsync(
                profile => profile.TenantProfiles!.Any(x => x.TenantId == tenantId), null, null, query => query.Include(x => x.TenantProfiles));
            foreach (var profile in profiles)
            {
                _profileRepository.Remove(profile);
            }
            await _profileRepository.SaveChangesAsync();

            // Delete schedules associated with tenant
            var schedules = await _scheduleRepository.FindAsync(p => p.TenantId == tenantId, string.Empty, null, null);

            foreach (var schedule in schedules)
            {
                _scheduleRepository.Remove(schedule);
            }
            await _scheduleRepository.SaveChangesAsync();

            _tenantRepository.Remove(tenant);

            await _tenantRepository.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<int[]> GetAllowedTenantIds(int userId)
        {
            var user = await _userRepository.FindByIdAsync(userId, null);
            if (user == null)
                throw new ObjectNotFoundException("User not found.");

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(userId);

            // Global admins can see data from non-isolated tenants. Others can see data from their tenants only.
            Expression<Func<Tenant, bool>> filterExpression;
            if (permissions.IsGlobalAdmin)
                filterExpression = tenant => !tenant.Isolated || tenant.Id == user.TenantId;
            else
                filterExpression = tenant => tenant.Id == user.TenantId;

            var tenants = await _tenantRepository.FindAsync(filterExpression, string.Empty, null, null);

            return tenants.Select(x => x.Id).ToArray();
        }

        public async Task<ImmutableHashSet<int>> GetTenantIdsWithJobs()
        {
            var backupJobs = await _jobRepository.FindAllAsync(null);

            var tenantIds = backupJobs.Select(x => x.TenantId).ToList();

            return tenantIds.ToImmutableHashSet();
        }

        private static void ValidateTenant(Tenant tenant)
        {
            string? message = null;

            if (string.IsNullOrWhiteSpace(tenant.Name))
                message = "Name cannot be empty.";

            if (message != null)
                throw new InvalidObjectException(message, nameof(tenant));
        }
    }
}
