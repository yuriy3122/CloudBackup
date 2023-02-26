
namespace CloudBackup.Model
{
    public class RestoreJobObject : EntityBase
    {
        public RestoreJob RestoreJob { get; set; } = null!;

        public int RestoreJobId { get; set; }

        public string ParentId { get; set; } = null!;

        public BackupObject? BackupObject { get; set; }

        public int? BackupObjectId { get; set; }

        public string NewObjectId { get; set; } = null!;

        public RestoreObjectStatus Status { get; set; }

        public string RestoreParams { get; set; } = null!;

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public string GroupGuid { get; set; } = null!;
    }

    public enum RestoreObjectStatus
    {
        Idle = 0,
        Failed = 1,
        Complete = 2
    }
}
