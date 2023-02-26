
namespace CloudBackup.Model
{
    public enum Severity : byte
    {
        Warning = 0,
        Error = 1,
        Info = 2
    }

    public class Log : EntityBase
    {
        public string ObjectId { get; set; } = null!;

        public string ObjectType { get; set; } = null!;

        public DateTime EventDate { get; set; }

        public string MessageText { get; set; } = null!;

        public string XmlData { get; set; } = null!;

        public Severity Severity { get; set; }

        public int? UserId { get; set; }
    }
}