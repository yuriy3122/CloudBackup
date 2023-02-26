using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class AlertRepository : RepositoryBase<Alert>
    {
        public AlertRepository(BackupContext context) : base(context)
        {
        }
    }
}
