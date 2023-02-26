using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class ScheduleRepository : RepositoryBase<Schedule>
    {
        public ScheduleRepository(BackupContext context) : base(context)
        {
        }
    }
}