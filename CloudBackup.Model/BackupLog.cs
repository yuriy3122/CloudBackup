
namespace CloudBackup.Model
{
    public class BackupLog : EntityBase
    {
        public int BackupId { get; set; }

        public DateTime EventDate { get; set; }

        public Severity Severity { get; set; }

        public string Message { get; set; } = null!;
    }
}
