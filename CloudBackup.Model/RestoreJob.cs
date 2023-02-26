
namespace CloudBackup.Model
{
    public class RestoreJob : EntityBase
    {
        public string Name { get; set; } = null!;

        public Schedule? Schedule { get; set; }

        public int? ScheduleId { get; set; }

        public RestoreJobStatus Status { get; set; }

        public RestoreMode RestoreMode { get; set; }

        public Tenant Tenant { get; set; } = null!;

        public int TenantId { get; set; }

        public User? User { get; set; }

        public int? UserId { get; set; }

        public List<RestoreJobObject> RestoreJobObjects { get; set; } = null!;

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public RestoreJobResult? Result { get; set; }
    }

    public enum RestoreJobStatus
    {
        Idle = 0,
        Running = 1,
        Complete = 2
    }

    public enum RestoreJobResult
    {
        Success = 0,
        Failed = 1
    }

    public enum RestoreMode
    {
        OriginalLocation = 0,
        NewLocation = 1
    }
}