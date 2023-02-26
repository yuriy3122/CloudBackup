using CloudBackup.Common;

namespace CloudBackup.Model
{
    public enum BackupStatus
    {
        Success,
        Failed,
        Warning,
        Running,
        Replicating
    }

    public class Backup : EntityBase
    {
        public string Name { get; set; } = null!;

        public Job Job { get; set; } = null!;

        public int JobId { get; set; }

        public bool IsPermanent { get; set; }

        public bool IsArchive { get; set; }

        public string JobConfiguration { get; set; } = null!;

        public List<BackupObject> BackupObjects { get; set; } = null!;

        public List<BackupLog> BackupLogs { get; set; } = null!;

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public BackupStatus? Status { get; set; }

        public string? StatusText => Status?.ToString();
    }

    public static class BackupExtensions
    {
        public static BackupStatus? GetStatus(this Backup backup)
        {
            if (backup.BackupObjects == null)
                return null;

            var logSeverities = backup.BackupLogs.Select(x => x.Severity).Distinct().ToHashSet();
            var backupObjectsByStatus = backup.BackupObjects.EmptyIfNull().ToLookup(x => x.Status);

            if (backupObjectsByStatus.Contains(BackupObjectStatus.Running))
            {
                var replicating = backupObjectsByStatus[BackupObjectStatus.Running].Any(x => x.Type == BackupObjectType.SnapshotCopy);

                return replicating ? BackupStatus.Replicating : BackupStatus.Running;
            }
            if (backupObjectsByStatus.Contains(BackupObjectStatus.Failed) || logSeverities.Contains(Severity.Error))
                return BackupStatus.Failed;
            if (backupObjectsByStatus.Contains(BackupObjectStatus.Warning) || logSeverities.Contains(Severity.Warning))
                return BackupStatus.Warning;
            if (backupObjectsByStatus.Contains(BackupObjectStatus.Success))
                return BackupStatus.Success;

            return null;
        }
    }
}