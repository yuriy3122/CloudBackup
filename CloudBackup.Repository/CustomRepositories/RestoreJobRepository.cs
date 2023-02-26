using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class RestoreJobRepository : RepositoryBase<RestoreJob>
    {
        public RestoreJobRepository(BackupContext context) : base (context)
        {
        }
    }
}