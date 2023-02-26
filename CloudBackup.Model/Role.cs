
namespace CloudBackup.Model
{
    public class Role : EntityBase
    {
        public string Name { get; set; } = null!;

        public List<UserRole> UserRoles { get; set; } = null!;

        /// <summary>
        /// If role represents global admin. Global admins have access to administrative menu.
        /// They also have access to other tenant's users.
        /// </summary>
        public bool IsGlobalAdmin { get; set; }

        /// <summary>
        /// If role represents user admin. User admins have access to administrative menu.
        /// </summary>
        public bool IsUserAdmin { get; set; }

        public Permissions ProfileRights { get; set; }

        public Permissions TenantRights { get; set; }

        public Permissions UserRights { get; set; }

        public Permissions InfrastructureRights { get; set; }

        public Permissions BackupRights { get; set; }

        public Permissions RestoreRights { get; set; }

        public Permissions SecurityRights { get; set; }

        public UserPermissions GetPermissions()
        {
            return new UserPermissions
            {
                IsGlobalAdmin = IsGlobalAdmin,
                IsUserAdmin = IsUserAdmin,
                ProfileRights = ProfileRights,
                TenantRights = TenantRights,
                UserRights = UserRights,
                InfrastructureRights = InfrastructureRights,
                BackupRights = BackupRights,
                RestoreRights = RestoreRights,
                SecurityRights = SecurityRights
            };
        }
    }
}
