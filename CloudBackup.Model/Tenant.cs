
namespace CloudBackup.Model
{
    public class Tenant : EntityBase
    {
        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public bool IsSystem { get; set; }

        public bool Isolated { get; set; }

        public List<TenantProfile> TenantProfiles { get; set; } = null!;

        public List<User> Users { get; set; } = null!;
    }
}
