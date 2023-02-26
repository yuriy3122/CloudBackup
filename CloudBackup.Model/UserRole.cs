
namespace CloudBackup.Model
{
    public class UserRole : EntityBase
    {
        public int RoleId { get; set; }

        public Role Role { get; set; } = null!;

        public int UserId { get; set; }

        public User User { get; set; } = null!;
    }
}