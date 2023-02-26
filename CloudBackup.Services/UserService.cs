using System.Collections.Immutable;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using CloudBackup.Common;
using CloudBackup.Common.Exceptions;
using CloudBackup.Model;
using CloudBackup.Repositories;

namespace CloudBackup.Services
{
    public interface IUserService
    {
        Task<User> GetUserByLoginAsync(string login);

        Task<ModelList<User>> GetUsersAsync(int userId, QueryOptions<User>? options = null);

        Task<User> GetUserAsync(int currentUserId, int id);

        Task<User> AddUserAsync(int userId, User user, string password);

        Task UpdateUserAsync(int currentUserId, int userId, User user, string newPassword);

        Task RemoveUserAsync(int currentUserId, int userId);

        /// <summary>
        /// Gets user ids which data can be accessed by a specified user.
        /// </summary>
        /// <param name="userId">ID of a user which allowed user's ids need to be returned.</param>
        Task<int[]> GetAllowedUserIds(int userId);

        Task<ImmutableHashSet<int>> GetUserIdsWithJobs();
    }

    public class UserService : IUserService
    {
        private const string DefaultOrder = nameof(User.Name);

        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly ITenantService _tenantService;
        private readonly IRepository<Job> _jobRepository;
        private readonly IRepository<RestoreJob> _restoreJobRepository;

        public UserService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRepository<Tenant> tenantRepository,
            ITenantService tenantService,
            IRepository<Job> jobRepository,
            IRepository<RestoreJob> restoreJobRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _tenantRepository = tenantRepository;
            _tenantService = tenantService;
            _jobRepository = jobRepository;
            _restoreJobRepository = restoreJobRepository;
        }

        public async Task<User> GetUserByLoginAsync(string login)
        {
            if (string.IsNullOrEmpty(login))
            {
                throw new ArgumentNullException(login);
            }

            var user = await _userRepository.GetUserByLoginAsync(login);

            if (user == null)
            {
                throw new ObjectNotFoundException("User doesn't exist.");
            }

            return user;
        }

        public async Task<ModelList<User>> GetUsersAsync(int userId, QueryOptions<User>? options = null)
        {
            Expression<Func<User, bool>> filterExpression = null!;

            if (options != null)
            {
                filterExpression = options.FilterExpression;
            }

            var currentUser = await _userRepository.FindByIdAsync(userId, null);

            if (currentUser == null)
            {
                throw new ObjectNotFoundException("Current user not found.");
            }

            // applying rights
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(currentUser.Id);

            if (!permissions.UserRights.HasFlag(Permissions.Read))
            {
                throw new UnauthorizedAccessException("No rights to view users.");
            }

            //  Users who are not global admin can only see users from the same tenants.
            if (!permissions.IsGlobalAdmin)
            {
                filterExpression = filterExpression.And(x => x.TenantId == currentUser.TenantId);
            }

            if (!string.IsNullOrEmpty(options?.Filter))
            {
                var filterLower = options.Filter.ToLower();
                filterExpression = filterExpression.And(
                    f => f.Name.ToLower().Contains(filterLower)
                         || f.Description.ToLower().Contains(filterLower)
                         || f.Login.ToLower().Contains(filterLower)
                         || f.Email.ToLower().Contains(filterLower)
                         || f.UserRoles.Any(x => x.Role.Name.ToLower().Contains(filterLower)));
            }

            var orderBy = string.IsNullOrEmpty(options?.OrderBy) ? DefaultOrder : options?.OrderBy;

            var users = await _userRepository.FindAsync(filterExpression, orderBy, options?.Page,
                u => u.Include(x => x.Tenant).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.Owner));

            var usersCount = await _userRepository.CountAsync(filterExpression, null);

            var usersList = ModelList.Create(users, usersCount);

            return usersList;
        }

        public async Task<User> GetUserAsync(int currentUserId, int id)
        {
            var currentUser = await _userRepository.FindByIdAsync(currentUserId, null);

            if (currentUser == null)
            {
                throw new ObjectNotFoundException("Current user not found.");
            }

            // applying rights
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(currentUser.Id);

            if (!permissions.UserRights.HasFlag(Permissions.Read))
            {
                throw new UnauthorizedAccessException("No rights to view users.");
            }

            var user = await _userRepository.FindByIdAsync(id,
                u => u.Include(x => x.Tenant).Include(x => x.UserRoles).ThenInclude(x => x.Role).Include(x => x.Owner));

            //  Users who are not global admin can only see users from the same tenants.
            if (!permissions.IsGlobalAdmin && user.TenantId != currentUser.TenantId)
            {
                throw new UnauthorizedAccessException("No rights to view users of another tenant.");
            }

            return user;
        }

        public async Task<User> AddUserAsync(int userId, User user, string password)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var currentUser = await _userRepository.FindByIdAsync(userId, null);

            // checking rights
            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(currentUser.Id);

            if (!permissions.UserRights.HasFlag(Permissions.Write))
            {
                throw new UnauthorizedAccessException("No rights to edit users.");
            }

            var userToAdd = new User
            {
                Name = user.Name,
                Description = user.Description,
                Login = user.Login,
                Email = user.Email,
                TenantId = user.TenantId != 0 ? user.TenantId : currentUser.TenantId,
                UserRoles = user.UserRoles?.Select(x => new UserRole { UserId = x.UserId, RoleId = x.RoleId }).ToList() ?? new List<UserRole>(),
                UtcOffset = user.UtcOffset,
                IsEnabled = user.IsEnabled,
                OwnerUserId = currentUser.Id
            };

            // tenant can be set only if user has access to tenants
            if (userToAdd.TenantId != currentUser.TenantId && !permissions.TenantRights.HasFlag(Permissions.Read))
            {
                throw new UnauthorizedAccessException("No access to tenants.");
            }

            // only admins can edit users of another tenants
            if (!permissions.IsGlobalAdmin && userToAdd.TenantId != currentUser.TenantId)
            {
                throw new UnauthorizedAccessException("No rights to change other tenant's data.");
            }

            // only admins can assign Global Admin role
            var roles = await _roleRepository.FindByIdsAsync(userToAdd.UserRoles.Select(x => x.RoleId).ToArray(), null);

            if (roles.Any(x => x.IsGlobalAdmin) && !permissions.IsGlobalAdmin)
            {
                throw new UnauthorizedAccessException("No rights to set Global Admin role.");
            }

            UpdateUserPassword(userToAdd, password);

            await ValidateUser(userToAdd, password);

            _userRepository.Add(userToAdd);

            await _userRepository.SaveChangesAsync();

            return userToAdd;
        }

        public async Task UpdateUserAsync(int currentUserId, int userId, User user, string newPassword)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var userToUpdate = await _userRepository.FindByIdAsync(userId, u => u.Include(x => x.Tenant).Include(x => x.UserRoles).ThenInclude(x => x.Role));

            if (userToUpdate == null)
            {
                throw new ObjectNotFoundException("There is no user with this id.");
            }

            // checking rights
            var currentUser = await _userRepository.FindByIdAsync(currentUserId, null);

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(currentUser.Id);

            if (!permissions.UserRights.HasFlag(Permissions.Write))
            {
                throw new UnauthorizedAccessException("No rights to edit users.");
            }

            if (userToUpdate.Login != user.Login)
            {
                throw new NotSupportedException("Can't edit user login.");
            }

            if (userToUpdate.TenantId != user.TenantId)
            {
                if (!permissions.TenantRights.HasFlag(Permissions.Read))
                {
                    throw new UnauthorizedAccessException("No access to tenants.");
                }

                // only admins can edit users of another tenants
                if (!permissions.IsGlobalAdmin && user.TenantId != currentUser.TenantId)
                {
                    throw new UnauthorizedAccessException("No rights to change user's tenant.");
                }
            }

            // only admins can assign/unassign Global Admin role
            var roles = (await _roleRepository.FindByIdsAsync(user.UserRoles.Select(x => x.RoleId).ToArray(), null)).ToList();

            if (roles.Any(x => x.IsGlobalAdmin) && !permissions.IsGlobalAdmin)
            {
                throw new UnauthorizedAccessException("No rights to set Global Admin role.");
            }

            if (currentUserId == userId && permissions.IsGlobalAdmin && !roles.Any(x => x.IsGlobalAdmin))
            {
                throw new NotSupportedException("Can't remove Global Administrator role from yourself.");
            }

            if (currentUserId == userId && permissions.IsUserAdmin && !roles.Any(x => x.IsGlobalAdmin || x.IsUserAdmin))
            {
                throw new NotSupportedException("Can't remove User Administrator role from yourself.");
            }

            if (currentUserId == userId && !user.IsEnabled)
            {
                throw new NotSupportedException("Can't disable yourself.");
            }

            userToUpdate.Name = user.Name;
            userToUpdate.Description = user.Description;
            userToUpdate.Login = user.Login;
            userToUpdate.Email = user.Email;
            userToUpdate.TenantId = user.TenantId;
            userToUpdate.UtcOffset = user.UtcOffset;
            userToUpdate.IsEnabled = user.IsEnabled;

            UpdateUserRoles(userToUpdate, user.UserRoles.Select(x => x.RoleId).ToArray());

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                UpdateUserPassword(userToUpdate, newPassword);
            }

            await ValidateUser(userToUpdate, newPassword, false);

            await _userRepository.SaveChangesAsync();
        }

        public async Task RemoveUserAsync(int currentUserId, int userId)
        {
            var user = await _userRepository.FindByIdAsync(userId, null);

            if (user == null)
            {
                throw new ObjectNotFoundException("There is no user with this id");
            }

            // checking rights
            var currentUser = await _userRepository.FindByIdAsync(currentUserId, null);

            if (userId == currentUser.Id)
            {
                throw new UnauthorizedAccessException("It is forbidden to delete yourself.");
            }

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(currentUser.Id);

            if (!permissions.UserRights.HasFlag(Permissions.Write))
            {
                throw new UnauthorizedAccessException("No rights to delete users.");
            }

            // only admins can edit users of another tenants
            if (!permissions.IsGlobalAdmin && user.TenantId != currentUser.TenantId)
            {
                throw new UnauthorizedAccessException("No rights to change other tenant's data.");
            }

            // Reset owned user manually because SQL don't support multiple CASCADE statements
            var ownedUsers = await _userRepository.FindAsync(x => x.OwnerUserId == userId, string.Empty, null, null);

            foreach (var ownedUser in ownedUsers)
            {
                ownedUser.OwnerUserId = null;
                _userRepository.Update(ownedUser);
            }

            await _userRepository.SaveChangesAsync();

            _userRepository.Remove(user);

            await _userRepository.SaveChangesAsync();
        }

        public async Task<int[]> GetAllowedUserIds(int userId)
        {
            var filterExpression = await GetAllowedUsersFilter(userId);

            var users = await _userRepository.FindAsync(filterExpression, string.Empty, null, null);

            return users.Select(x => x.Id).ToArray();
        }

        public async Task<ImmutableHashSet<int>> GetUserIdsWithJobs()
        {
            var backupJobs = await _jobRepository.FindAllAsync(null);
            var restoreJobs = await _restoreJobRepository.FindAllAsync(null);

#pragma warning disable CS8629 // Nullable value type may be null.
            return backupJobs.Select(x => x.UserId)
                .Union(restoreJobs.Select(x => x.UserId))
                .Where(x => x != null)
                .Select(x => x.Value)
                .ToImmutableHashSet();
#pragma warning restore CS8629 // Nullable value type may be null.
        }

        private async Task<Expression<Func<User, bool>>> GetAllowedUsersFilter(int userId)
        {
            var allowedTenantIds = await _tenantService.GetAllowedTenantIds(userId);

            return user => allowedTenantIds.Contains(user.TenantId);
        }

        private static void UpdateUserRoles(User user, ICollection<int> roleIds)
        {
            var currentRoleIds = user.UserRoles.Select(x => x.RoleId).ToImmutableHashSet();

            if (currentRoleIds == null)
            {
                return;
            }

            var newRolesIds = roleIds.Where(id => !currentRoleIds.Contains(id));
            var newUserRoles = newRolesIds.Select(roleId => new UserRole { UserId = user.Id, RoleId = roleId });
            user.UserRoles.AddRange(newUserRoles);

            var removedRoleIds = currentRoleIds.Except(roleIds).ToImmutableHashSet();
            var rolesToRemove = user.UserRoles.Where(x => removedRoleIds.Contains(x.RoleId)).ToList();

            foreach (var roleToRemove in rolesToRemove)
            {
                user.UserRoles.Remove(roleToRemove);
            }
        }

        private static void UpdateUserPassword(User user, string password)
        {
            var data = PasswordHelper.GetPasswordHashSalt(password);
            user.PasswordHash = data.Hash;
            user.PasswordSalt = data.Salt;
        }

        private async Task ValidateUser(User user, string password, bool checkPassword = true)
        {
            if (string.IsNullOrWhiteSpace(user.Name))
            {
                throw new InvalidObjectException("Name cannot be empty.", nameof(user));
            }

            if (string.IsNullOrWhiteSpace(user.Login))
            {
                throw new InvalidObjectException("Login cannot be empty.", nameof(user));
            }

            if (!RegexExpressions.LettersNumbersUnderscoresRegex.IsMatch(user.Login))
            {
                throw new InvalidObjectException("Login must contain only latin characters, numbers or underscores.", nameof(user));
            }

            var haveUsersWithSameLogin = (await _userRepository.FindAsync(x => x.Login == user.Login && x.Id != user.Id, 
                string.Empty, null, null)).Any();

            if (haveUsersWithSameLogin)
            {
                throw new InvalidObjectException("This login is already in use.", nameof(user));
            }

            if (!RegexExpressions.EmailRegex.IsMatch(user.Email))
            {
                throw new InvalidObjectException("Email is not correct.", nameof(user));
            }

            if (checkPassword)
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidObjectException("Password cannot be empty.", nameof(user));
                }

                if (!RegexExpressions.LettersNumbersUnderscoresRegex.IsMatch(password))
                {
                    throw new InvalidObjectException("Password must contain only latin characters, numbers or underscores.", nameof(user));
                }
            }

            // If user has rights to edit tenants, a tenant must be selected. If user doesn't have rights, current tenant is used.
            if (user.TenantId == 0)
            {
                throw new InvalidObjectException("A tenant must be selected.", nameof(user));
            }

            if (!user.UserRoles.Any())
            {
                throw new InvalidObjectException("A role must be selected.", nameof(user));
            }

            // Global admin user must be in system tenant.
            var roles = await _roleRepository.FindByIdsAsync(user.UserRoles.Select(x => x.RoleId).ToList(), null);

            if (roles.Any(x => x.IsGlobalAdmin))
            {
                var tenant = await _tenantRepository.FindByIdAsync(user.TenantId, null);

                if (!tenant.IsSystem)
                {
                    throw new NotSupportedException("Users with global admin role must be in a system tenant.");
                }
            }
        }
    }
}