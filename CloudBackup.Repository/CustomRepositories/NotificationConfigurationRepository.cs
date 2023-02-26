using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class NotificationConfigurationRepository : RepositoryBase<NotificationConfiguration>
    {
        public NotificationConfigurationRepository(BackupContext context) : base(context)
        {
        }
    }
}
