using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class JobRepository : RepositoryBase<Job>
    {
        public JobRepository(BackupContext context) : base(context)
        {
        }
    }
}