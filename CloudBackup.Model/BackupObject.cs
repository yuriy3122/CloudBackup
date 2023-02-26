
namespace CloudBackup.Model
{
    public class BackupObject : EntityBase
    {
        public Backup Backup { get; set; } = null!;

        public int BackupId { get; set; }

        public BackupObjectType Type { get; set; }

        public Profile Profile { get; set; } = null!;

        public int ProfileId { get; set; }

        public string Region { get; set; } = null!;

        public string ParentId { get; set; } = null!;

        public string SourceObjectId { get; set; } = null!;

        public string DestObjectId { get; set; } = null!;

        public string FolderId { get; set; } = null!;

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public BackupObjectStatus Status { get; set; }

        public string BackupParams { get; set; } = null!;

        public JobObjectType GetJobObjectType()
        {
            switch (Type)
            {
                case BackupObjectType.Snapshot:
                case BackupObjectType.SnapshotCopy:
                    return ParentId != null ? JobObjectType.Instance : JobObjectType.Volume;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool IsReplication()
        {
            return Type == BackupObjectType.SnapshotCopy;
        }
    }

    public enum BackupObjectType
    {
        Snapshot = 0,
        Ami = 1,
        SnapshotCopy = 2
    }

    public enum BackupObjectStatus
    {
        Success = 0,
        Failed = 1,
        Warning = 2,
        Running = 3
    }
}