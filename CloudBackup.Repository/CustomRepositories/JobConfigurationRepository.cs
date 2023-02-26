using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class JobConfigurationRepository : RepositoryBase<JobConfiguration>
    {
        public JobConfigurationRepository(BackupContext context) : base (context)
        {
        }
    }
}