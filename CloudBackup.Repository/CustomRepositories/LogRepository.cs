using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class LogRepository : RepositoryBase<Log>
    {
        public LogRepository(BackupContext context) : base (context)
        {
        }
    }
}