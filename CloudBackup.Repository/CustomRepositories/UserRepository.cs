using Microsoft.EntityFrameworkCore;
using CloudBackup.Common.Exceptions;
using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetUserByLoginAsync(string login);
    }

    public class UserRepository : RepositoryBase<User>, IUserRepository
    {
        public UserRepository(BackupContext context) : base(context)
		{
        }

        public async Task<User> GetUserByLoginAsync(string login)
        {
            var user = await Context.Users
                .Include(x => x.Tenant)
                .Include(x => x.UserRoles).ThenInclude(x => x.Role)
                .Include(x => x.Owner)
                .Where(x => x.Login == login)
                .SingleOrDefaultAsync();

            if (user == null)
            {
                throw new ObjectNotFoundException($"There is no user with login='{login}'");
            }

            return user;
        }
    }
}