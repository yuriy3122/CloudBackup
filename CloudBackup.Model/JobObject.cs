namespace CloudBackup.Model
{
    public class JobObject : EntityBase
    {
        public Job Job { get; set; } = null!;

        public int JobId { get; set; }

        public JobObjectType Type { get; set; }

        public string ParentId { get; set; } = null!;

        public Profile Profile { get; set; } = null!;

        public int ProfileId { get; set; }

        public string Region { get; set; } = null!;

        public string ObjectId { get; set; } = null!;

        public string CustomData { get; set; } = null!;

        public string FolderId { get; set; } = null!;
    }

    public enum JobObjectType
    {
        Instance = 0,
        Volume = 1
    }
}