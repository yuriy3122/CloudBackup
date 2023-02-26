using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBackup.Model
{
    public class User : EntityBase
    {
        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public Tenant Tenant { get; set; } = null!;

        public int TenantId { get; set; }

        public string Login { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string PasswordSalt { get; set; } = null!;

        public bool IsEnabled { get; set; }

        public int? OwnerUserId { get; set; }

        [NotMapped]
        public TimeSpan UtcOffset
        {
            get { return TimeSpan.FromTicks(UtcOffsetTicks); }
            set { UtcOffsetTicks = value.Ticks; }
        }

        public long UtcOffsetTicks { get; set; }

        public List<UserRole> UserRoles { get; set; } = null!;

        public User Owner { get; set; } = null!;

        public Role? Role => UserRoles?.FirstOrDefault()?.Role;
    }
}