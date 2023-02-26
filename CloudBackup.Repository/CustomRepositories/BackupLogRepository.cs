using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class BackupLogRepository : RepositoryBase<BackupLog>
    {
        public BackupLogRepository(BackupContext context) : base(context)
        {
        }
    }
}