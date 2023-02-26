using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Linq.Expressions;
using CloudBackup.Common;
using CloudBackup.Common.Exceptions;
using CloudBackup.Model;
using CloudBackup.Repositories;

namespace CloudBackup.Services
{
    public interface IProfileService
    {
        Task<ModelList<Profile>> GetProfilesAsync(int userId, QueryOptions<Profile>? options = null);

        Task<Profile> GetProfileAsync(int userId, int profileId);

        Task<Profile> AddProfileAsync(int userId, Profile profile);

        Task UpdateProfileAsync(int userId, int profileId, Profile profile);

        Task RemoveProfileAsync(int userId, int profileId);

        /// <summary>
        /// Gets profile ids which data can be accessed by a specified user.
        /// </summary>
        /// <param name="userId">ID of a user which profile's ids need to be returned.</param>
        Task<int[]> GetAllowedProfileIds(int userId);

        Task<ImmutableHashSet<int>> GetProfileIdsWithJobs();
    }

    public class ProfileService : IProfileService
    {
        private const string DefaultOrder = nameof(Profile.Name);

        private readonly IRepository<Profile> _profileRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRepository<JobObject> _jobObjectRepository;
        private readonly IRepository<BackupObject> _backupObjectRepository;
        private readonly ITenantService _tenantService;

        public ProfileService(
            IRepository<Profile> profileRepository,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRepository<JobObject> jobObjectRepository,
            IRepository<BackupObject> backupObjectRepository,
            ITenantService tenantService)
        {
            _profileRepository = profileRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _jobObjectRepository = jobObjectRepository;
            _backupObjectRepository = backupObjectRepository;
            _tenantService = tenantService;
        }

        public async Task<ModelList<Profile>> GetProfilesAsync(int userId, QueryOptions<Profile>? options = null)
        {
            Expression<Func<Profile, bool>> filterExpression = null!;

            if (options != null)
            {
                filterExpression = options.FilterExpression;
            }

            // applying rights
            var user = await _userRepository.FindByIdAsync(userId, null);

            if (user == null)
            {
                throw new ObjectNotFoundException("User not found.");
            }

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(user.Id);
            if (!permissions.ProfileRights.HasFlag(Permissions.Read))
                throw new UnauthorizedAccessException("No rights to view AWS accounts.");

            // filter by allowed profiles
            var allowedFilterExpression = await GetAllowedProfilesFilter(userId);
            filterExpression = filterExpression.And(allowedFilterExpression);

            if (!string.IsNullOrEmpty(options?.Filter))
            {
                var filterLower = options.Filter.ToLower();

                filterExpression = filterExpression.And(f => f.Name != null && f.Name.ToLower().Contains(filterLower)
                         || f.Description != null && f.Description.ToLower().Contains(filterLower)
                         || f.Owner != null && f.Owner.Name.ToLower().Contains(filterLower));
            }

            var profiles = (await _profileRepository.FindAsync(filterExpression, 
                options?.OrderBy ?? DefaultOrder, options?.Page,
                p => p.Include(x => x.Owner!).Include(x => x.TenantProfiles!).ThenInclude(x => x.Tenant))).ToList();

            // convert dates to user's time
            foreach (var profile in profiles)
            {
                if (profile.Created - DateTime.MinValue > -user.UtcOffset && DateTime.MaxValue - profile.Created > user.UtcOffset)
                    profile.Created += user.UtcOffset;
            }

            var profilesCount = await _profileRepository.CountAsync(filterExpression, null);

            var profilesList = ModelList.Create(profiles, profilesCount);

            return profilesList;
        }

        public async Task<Profile> GetProfileAsync(int userId, int profileId)
        {
            // applying rights
            var user = await _userRepository.FindByIdAsync(userId, null);
            if (user == null)
                throw new ObjectNotFoundException("User not found.");

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(user.Id);
            if (!permissions.ProfileRights.HasFlag(Permissions.Read))
                throw new UnauthorizedAccessException("No rights to view AWS accounts.");


            var allowedProfileIds = await GetAllowedProfileIds(userId);

            if (!allowedProfileIds.Contains(profileId))
            {
                throw new UnauthorizedAccessException("No access to this AWS account.");
            }

            var profile = await _profileRepository.FindByIdAsync(profileId,
                p => p.Include(x => x.Owner!).Include(x => x.TenantProfiles!).ThenInclude(x => x.Tenant));

            if (profile == null)
                throw new ObjectNotFoundException("AWS account not found.");

            return profile;
        }

        public async Task<Profile> AddProfileAsync(int userId, Profile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var user = await _userRepository.FindByIdAsync(userId, null);
            if (user == null)
                throw new ObjectNotFoundException("User not found.");

            // checking rights
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(user.Id);
            if (!permissions.ProfileRights.HasFlag(Permissions.Write))
                throw new UnauthorizedAccessException("No rights to edit AWS accounts.");

            var profileToAdd = new Profile
            {
                Name = profile.Name,
                Description = string.Empty,
                ServiceAccountId = profile.ServiceAccountId,
                KeyId = profile.KeyId,
                PrivateKey = profile.PrivateKey?.Replace("\n", "").Replace("\r", ""),
                AuthenticationType = AuthenticationType.IAMUser,
                OwnerUserId = userId,
                Created = DateTime.UtcNow,
                State = ProfileState.Valid,
                TenantProfiles = profile.TenantProfiles!
                    .EmptyIfNull()
                    .Select(x => new TenantProfile { ProfileId = x.ProfileId, TenantId = x.TenantId })
                    .ToList()
            };

            // if tenant is not set, assign user's current tenant.
            if (!profileToAdd.TenantProfiles.Any())
            {
                profileToAdd.TenantProfiles = new List<TenantProfile> { new TenantProfile { Profile = profile, TenantId = user.TenantId } };
            }

            ValidateProfile(profileToAdd);

            var allowedTenantIds = await _tenantService.GetAllowedTenantIds(user.Id);
            if (!profileToAdd.TenantProfiles.Any(x => allowedTenantIds.Contains(x.TenantId)))
                throw new UnauthorizedAccessException("No rights to add profiles to this tenant.");

            var duplicates = await _profileRepository.FindAsync(x => x.ServiceAccountId == profileToAdd.ServiceAccountId, 
                string.Empty, null, null);

            if (duplicates.Any())
                throw new NotSupportedException("Can't add more than profiles with the same service account.");

            profileToAdd.State = ProfileState.Valid;

            _profileRepository.Add(profileToAdd);

            await _profileRepository.SaveChangesAsync();

            return profileToAdd;
        }

        public async Task UpdateProfileAsync(int userId, int profileId, Profile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var profileToUpdate = await _profileRepository.FindByIdAsync(
                profileId, u => u.Include(x => x.TenantProfiles!).ThenInclude(x => x.Tenant));
            if (profileToUpdate == null)
                throw new ObjectNotFoundException("There is no AWS accounts with this id");

            var user = await _userRepository.FindByIdAsync(userId, null);
            if (user == null)
                throw new ObjectNotFoundException("User not found.");

            // checking rights
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(user.Id);
            if (!permissions.ProfileRights.HasFlag(Permissions.Write))
                throw new UnauthorizedAccessException("No rights to edit AWS accounts.");

            // check tenant rights
            var allowedTenantIds = await _tenantService.GetAllowedTenantIds(user.Id);
            if (!profileToUpdate.TenantProfiles!.Any(x => allowedTenantIds.Contains(x.TenantId)))
                throw new UnauthorizedAccessException("No rights to edit AWS accounts of this tenant.");

            ValidateProfile(profile);

            profileToUpdate.Name = profile.Name;
            profileToUpdate.KeyId = profile.KeyId;
            profileToUpdate.ServiceAccountId = profile.ServiceAccountId;
            profileToUpdate.PrivateKey = profile.PrivateKey?.Replace("\n", "").Replace("\r", "");

            var profileIdsWithJobs = await GetProfileIdsWithJobs();
            var hasJobs = profileIdsWithJobs.Contains(profileId);

            if (hasJobs && profileToUpdate.ServiceAccountId != null && profile.ServiceAccountId != profileToUpdate.ServiceAccountId)
            {
                throw new NotSupportedException("Can't change ServiceAccount when ServiceAccount is used in any jobs.");
            }

            var currentTenants = profileToUpdate.TenantProfiles!.Select(x => x.TenantId).ToImmutableHashSet();
            if (hasJobs && !currentTenants.SetEquals(profile.TenantProfiles!.Select(x => x.TenantId)))
                throw new NotSupportedException("Can't change tenant for accounts used in any jobs.");

            await _profileRepository.SaveChangesAsync();
        }

        public async Task RemoveProfileAsync(int userId, int profileId)
        {
            var profile = await _profileRepository.FindByIdAsync(profileId, profiles => profiles.Include(x => x.TenantProfiles));
            if (profile == null)
                throw new ObjectNotFoundException("There is no AWS accounts with this id");

            var user = await _userRepository.FindByIdAsync(userId, null);
            if (user == null)
                throw new ObjectNotFoundException("User not found.");

            // checking rights
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(user.Id);
            if (!permissions.ProfileRights.HasFlag(Permissions.Write))
                throw new UnauthorizedAccessException("No rights to edit AWS accounts.");

            var allowedTenantIds = await _tenantService.GetAllowedTenantIds(user.Id);
            if (!profile.TenantProfiles!.Any(x => allowedTenantIds.Contains(x.TenantId)))
                throw new UnauthorizedAccessException("No rights to edit AWS accounts of this tenant.");

            if (profile.IsSystem)
                throw new NotSupportedException("Can't delete system account.");

            var profileIdsWithJobs = await GetProfileIdsWithJobs();
            if (profileIdsWithJobs.Contains(profileId))
                throw new NotSupportedException("Can't delete account which is used in any jobs.");

            _profileRepository.Remove(profile);

            await _profileRepository.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<int[]> GetAllowedProfileIds(int userId)
        {
            var filterExpression = await GetAllowedProfilesFilter(userId);

            var profiles = await _profileRepository.FindAsync(filterExpression, string.Empty, null, null);

            return profiles.Select(x => x.Id).ToArray();
        }

        public async Task<ImmutableHashSet<int>> GetProfileIdsWithJobs()
        {
            var jobObjects = await _jobObjectRepository.FindAllAsync(null);
            var backupObjects = await _backupObjectRepository.FindAllAsync(null);

            return jobObjects.Select(x => x.ProfileId)
                .Union(backupObjects.Select(x => x.ProfileId))
                .ToImmutableHashSet();
        }

        private async Task<Expression<Func<Profile, bool>>> GetAllowedProfilesFilter(int userId)
        {
            var allowedTenantIds = await _tenantService.GetAllowedTenantIds(userId);

            return profile => profile.TenantProfiles!.Any(x => allowedTenantIds.Contains(x.TenantId));
        }

        private static void ValidateProfile(Profile profile)
        {
            string message = string.Empty;

            if (string.IsNullOrWhiteSpace(profile.Name))
                message = "Name cannot be empty.";

            if (profile.AuthenticationType == AuthenticationType.IAMUser)
            {
                if (string.IsNullOrWhiteSpace(profile.PrivateKey))
                {
                    message = "Private key cannot be empty.";
                }
                if (string.IsNullOrWhiteSpace(profile.ServiceAccountId))
                {
                    message = "Service Account must be specified";
                }
            }

            if (profile.TenantProfiles?.Count > 1)
            {
                message = "Only one tenant must be selected.";
            }

            if (!string.IsNullOrEmpty(message))
            {
                throw new InvalidObjectException(message, nameof(profile));
            }
        }

        private static void UpdateProfileTenants(Profile profile, IReadOnlyCollection<TenantProfile> newData)
        {
            var currentTenantIds = profile.TenantProfiles!.Select(x => x.TenantId).ToImmutableHashSet();

            var newTenants = newData.EmptyIfNull().Where(x => !currentTenantIds.Contains(x.TenantId));
            var newTenantProfiles = newTenants.Select(tenant => new TenantProfile { ProfileId = profile.Id, TenantId = tenant.TenantId }).ToList();

            var removedTenantIds = currentTenantIds.Except(newData.Select(x => x.TenantId)).ToImmutableHashSet();
            var tenantsToRemove = profile.TenantProfiles!.Where(x => removedTenantIds.Contains(x.TenantId)).ToList();

            profile.TenantProfiles!.AddRange(newTenantProfiles);

            foreach (var tenantToRemove in tenantsToRemove)
            {
                profile.TenantProfiles.Remove(tenantToRemove);
            }
        }
    }
}
