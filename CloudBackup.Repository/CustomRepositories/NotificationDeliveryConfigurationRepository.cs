using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class NotificationDeliveryConfigurationRepository : RepositoryBase<NotificationDeliveryConfiguration>
    {
        public NotificationDeliveryConfigurationRepository(BackupContext context) : base(context)
        {
        }
    }
}