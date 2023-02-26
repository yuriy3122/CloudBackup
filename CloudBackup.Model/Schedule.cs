
namespace CloudBackup.Model
{
    public class Schedule : EntityBase
    {
        public string Name { get; set; } = null!;

        public StartupType StartupType { get; set; }

        public OccurType? OccurType { get; set; }

        public Tenant Tenant { get; set; } = null!;

        public int TenantId { get; set; }

        /// <summary>
        /// Params in JSON format, depends on StartupType and OccurType. Example: {"time": "03:00:00.000"}
        /// </summary>
        public string Params { get; set; } = null!;

        public DateTime Created { get; set; }

        public int JobCount { get; set; } // Filled by raw sql in repository
    }

    public enum StartupType
    {
        Immediately = 0,
        Delayed = 1,
        Recurring = 2
    }

    public enum OccurType
    {
        Unknown = -1,
        Daily = 0,
        Periodically = 1,
        Monthly = 2
    }
}
