
namespace CloudBackup.Model
{
    public class UserPermissions
    {
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

        public UserPermissions Combine(UserPermissions other)
        {
            var result = Clone();

            if (other != null)
            {
                result.IsGlobalAdmin |= other.IsGlobalAdmin;
                result.IsUserAdmin |= other.IsUserAdmin;
                result.ProfileRights |= other.ProfileRights;
                result.TenantRights |= other.TenantRights;
                result.UserRights |= other.UserRights;
                result.InfrastructureRights |= other.InfrastructureRights;
                result.BackupRights |= other.BackupRights;
                result.RestoreRights |= other.RestoreRights;
                result.SecurityRights |= other.SecurityRights;
            }

            return result;
        }

        public static UserPermissions Combine(UserPermissions permissions1, UserPermissions permissions2)
        {
            if (permissions1 == null)
                throw new ArgumentNullException(nameof(permissions1));

            return permissions1.Combine(permissions2);
        }

        public UserPermissions Clone()
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

    [Flags]
    public enum Permissions : byte
    {
        None = 0x0,
        Read = 0x1,
        Write = 0x2,
        ReadWrite = Read | Write
    }
}
