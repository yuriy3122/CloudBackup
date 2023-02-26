using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class UserAlertRepository : RepositoryBase<UserAlert>
    {
        public UserAlertRepository(BackupContext context) : base(context)
        {
        }
    }
}