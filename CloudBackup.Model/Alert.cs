namespace CloudBackup.Model
{
    public enum AlertType : byte
    {
        Info,
        Warning,
        Error
    }

    public class Alert : EntityBase
    {
        public DateTime Date { get; set; }

        public AlertType Type { get; set; }

        public string? Message { get; set; }

        public string? Subject { get; set; }

        public int? LogId { get; set; }

        public Log? Log { get; set; }

        public bool IsProcessed { get; set; }

        public string? SourceObjectType { get; set; }

        public int SourceObjectId { get; set; }

        public List<UserAlert>? UserAlerts { get; set; }
    }
}