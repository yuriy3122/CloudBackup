using Microsoft.EntityFrameworkCore;
using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<UserPermissions> GetPermissionsByUserIdAsync(int userId);

        Task<UserPermissions> GetPermissionsByLoginAsync(string login);
    }

    public class RoleRepository : RepositoryBase<Role>, IRoleRepository
    {
        public RoleRepository(BackupContext context) : base(context)
        {
        }

        public async Task<UserPermissions> GetPermissionsByUserIdAsync(int userId)
        {
            var roles = await Context.UserRoles.Include(x => x.Role)
                .Where(x => x.UserId == userId)
                .Select(x => x.Role)
                .ToListAsync();

            var permissions = roles.Select(x => x.GetPermissions()).Aggregate((x, y) => x.Combine(y));

            return permissions;
        }

        public async Task<UserPermissions> GetPermissionsByLoginAsync(string login)
        {
            var userId = await Context.Users.Where(x => x.Login == login).Select(x => x.Id).SingleOrDefaultAsync();

            if (userId == 0)
            {
                throw new Exception("The specified login doesn't exist.");
            }

            return await GetPermissionsByUserIdAsync(userId);
        }
    }
}
