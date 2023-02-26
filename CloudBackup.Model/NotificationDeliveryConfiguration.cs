
namespace CloudBackup.Model
{
    public enum DeliveryMethod
    {
        Smtp,
        Sms
    }

    public class NotificationDeliveryConfiguration : EntityBase
    {
        public string Name { get; set; } = null!;

        public DeliveryMethod DeliveryMethod { get; set; }

        public Tenant Tenant { get; set; } = null!;

        public int TenantId { get; set; }

        public string Configuration { get; set; } = null!;
    }
}