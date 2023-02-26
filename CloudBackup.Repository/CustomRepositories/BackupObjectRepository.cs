using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class BackupObjectRepository : RepositoryBase<BackupObject>
    {
        public BackupObjectRepository(BackupContext context) : base (context)
        {
        }
    }
}