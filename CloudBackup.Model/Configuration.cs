
namespace CloudBackup.Model
{
    public class Configuration : EntityBase
    {
        public string InstanceId { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public long UtcOffsetTicks { get; set; }

        public ConfigurationStatus ConfigurationStatus { get; set; }

        public string? ErrorMessage { get; set; }
    }

    public enum ConfigurationStatus
    {
        Created = 0,
        Processed = 1,
        Failed = 2
    }
}
