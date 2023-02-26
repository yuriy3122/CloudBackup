using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class JobObjectRepository : RepositoryBase<JobObject>
    {
        public JobObjectRepository(BackupContext context) : base(context)
        {
        }
    }
}