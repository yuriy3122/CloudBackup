
namespace CloudBackup.Model
{
    public class TenantProfile : EntityBase
    {
        public int TenantId { get; set; }

        public Tenant Tenant { get; set; } = null!;

        public int ProfileId { get; set; }

        public Profile Profile { get; set; } = null!;
    }
}