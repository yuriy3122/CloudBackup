using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class UserRoleRepository : RepositoryBase<UserRole>
    {
        public UserRoleRepository(BackupContext context) : base(context)
        {
        }
    }
}