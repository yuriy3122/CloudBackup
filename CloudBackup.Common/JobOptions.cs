
namespace CloudBackup.Common
{
    public class JobOptions
    {
        public RetentionPolicy RetentionPolicy { get; set; }

        public ReplicationOptions ReplicationOptions { get; set; }

        public InstanceBackupMode InstanceBackupMode { get; set; }

        public BackupAfterErrorStatus StatusAfterScriptError { get; set; } = BackupAfterErrorStatus.Warning;

        public BackupAfterErrorStatus StatusAfterReplicationError { get; set; } = BackupAfterErrorStatus.Failed;

        public JobOptions()
        {
            RetentionPolicy = new RetentionPolicy();
            ReplicationOptions = new ReplicationOptions();
            TagJoinType = TagJoinType.Or;
        }

        public TagJoinType TagJoinType { get; set; }
    }

    public enum BackupAfterErrorStatus
    {
        Failed = 0,
        Warning = 1
    }

    public enum TagJoinType : byte
    {
        And = 0,
        Or = 1
    }

    public enum InstanceBackupMode
    {
        Default = 0, // "Snapshots + Initial AMI" for Windows, "Snapshots Only" for Linux
        [Description("AMI Only")]
        AmiOnly = 1
    }

    public class ReplicationOptions
    {
        public bool EnableReplication { get; set; }

        public int BackupInverval { get; set; }

        public List<string> ReplicationRegions { get; set; }

        public ReplicationOptions()
        {
            ReplicationRegions = new List<string>();
        }
    }

    public class RetentionPolicy
    {
        public RetentionPolicy()
        {
            RestorePointsToKeep = 10;
            TimeIntervalValue = 14;
            RetentionTimeIntervalType = RetentionTimeIntervalTypes.Days;
            RetentionAction = RetentionAction.RemoveBackup;
        }

        public TimeSpan GetTimeSpan()
        {
            if (RetentionTimeIntervalType == RetentionTimeIntervalTypes.Days)
            {
                return TimeSpan.FromDays(TimeIntervalValue);
            }
            else if (RetentionTimeIntervalType == RetentionTimeIntervalTypes.Hours)
            {
                return TimeSpan.FromHours(TimeIntervalValue);
            }
            else if (RetentionTimeIntervalType == RetentionTimeIntervalTypes.Months)
            {
                return TimeSpan.FromDays(TimeIntervalValue * 31);
            }

            return new TimeSpan();
        }

        public int RestorePointsToKeep { get; set; }

        public int TimeIntervalValue { get; set; }

        public RetentionTimeIntervalTypes RetentionTimeIntervalType { get; set; }

        public RetentionAction RetentionAction { get; set; }
    }

    public enum RetentionAction
    {
        RemoveBackup = 0,
        ArchiveBackup = 1
    }

    public enum RetentionTimeIntervalTypes
    {
        Days = 0,
        Hours = 1,
        Months = 2
    }
}