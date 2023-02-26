
namespace CloudBackup.Model
{
    public class Job : EntityBase
    {
        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public JobType Type { get; set; }

        public JobStatus Status { get; set; }

        public Schedule Schedule { get; set; } = null!;

        public int ScheduleId { get; set; }

        public Tenant Tenant { get; set; } = null!;

        public int TenantId { get; set; }

        public User? User { get; set; }

        public int? UserId { get; set; }

        public DateTime? LastRunAt { get; set; }

        public DateTime? NextRunAt { get; set; }

        public List<JobObject> JobObjects { get; set; } = null!;

        public int? ObjectCount { get; set; }

        //Forced Backup trigged
        public bool RunNow { get; set; }

        //Runs delayed job in scheduled time, after "Run Now" trigged
        public bool RunDelayed { get; set; }

        public JobConfiguration Configuration { get; set; } = null!;
    }

    public enum JobType
    {
        Backup = 0,
        BackupAndReplication = 1
    }

    public enum JobStatus
    {
        Idle = 0,
        Running = 1,
        Stopped = 2
    }
}