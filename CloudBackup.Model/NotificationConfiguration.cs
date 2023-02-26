
namespace CloudBackup.Model
{
    public enum NotificationType
    {
        Alert,
        DailySummary
    }

    public class NotificationConfiguration : EntityBase
    {
        public string? Name { get; set; }

        public int TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        public TimeSpan SendTime { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public int NotificationDeliveryConfigurationId { get; set; }

        public NotificationDeliveryConfiguration? NotificationDeliveryConfiguration { get; set; }

        public NotificationType Type { get; set; }

        public bool IsEnabled { get; set; }

        public bool IncludeTenants { get; set; }
    }
}