using CloudBackup.Common;

namespace CloudBackup.Model
{
    public enum ProfileState : byte
    {
        Invalid,
        Valid
    }

    public enum AuthenticationType
    {
        /// <summary>
        /// Service account, authentication using access and secret keys
        /// </summary>
        [Description("IAM User")]
        IAMUser = 0,
    }

    public class Profile : EntityBase
    {
        public AuthenticationType AuthenticationType { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? ServiceAccountId { get; set; }

        public string? KeyId { get; set; }

        public string? PrivateKey { get; set; }

        public ProfileState State { get; set; }

        public DateTime Created { get; set; }

        public int OwnerUserId { get; set; }

        public User? Owner { get; set; }

        public bool IsSystem { get; set; }

        public List<TenantProfile>? TenantProfiles { get; set; }

        public Tenant? Tenant => TenantProfiles?.FirstOrDefault()?.Tenant;

        public string AuthTypeAsString
        {
            get
            {
                return EnumHelper.GetEnumDescription(AuthenticationType);
            }
        }
    }
}