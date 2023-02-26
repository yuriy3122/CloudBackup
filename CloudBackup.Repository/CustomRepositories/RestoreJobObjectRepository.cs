using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class RestoreJobObjectRepository : RepositoryBase<RestoreJobObject>
    {
        public RestoreJobObjectRepository(BackupContext context) : base(context)
        {
        }
    }
}