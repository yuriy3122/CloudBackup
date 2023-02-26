using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class BackupRepository : RepositoryBase<Backup>
    {
        public BackupRepository(BackupContext context) : base (context)
        {
        }
    }
}
