export enum CustomPermissions {
    None = 0,
    Read = 1 << 0,
    Write = 1 << 1,
    ReadWrite = Read | Write
}

export class UserPermissions {
    public IsGlobalAdmin = false;
    public IsUserAdmin = false;
    public ProfileRights = CustomPermissions.None;
    public TenantRights = CustomPermissions.None;
    public UserRights = CustomPermissions.None;
    public InfrastructureRights = CustomPermissions.None;
    public BackupRights = CustomPermissions.None;
    public RestoreRights = CustomPermissions.None;
    public SecurityRights = CustomPermissions.None;


    static Copy(obj: any): UserPermissions {
        let copy = new UserPermissions();
        copy.IsGlobalAdmin = obj.IsGlobalAdmin || obj.isGlobalAdmin;
        copy.IsUserAdmin = obj.IsUserAdmin || obj.isUserAdmin;
        copy.ProfileRights = obj.ProfileRights || obj.profileRights || CustomPermissions.None;
        copy.TenantRights = obj.TenantRights || obj.tenantRights || CustomPermissions.None;
        copy.UserRights = obj.UserRights || obj.userRights || CustomPermissions.None;
        copy.InfrastructureRights = obj.InfrastructureRights || obj.infrastructureRights || CustomPermissions.None;
        copy.BackupRights = obj.BackupRights || obj.backupRights || CustomPermissions.None;
        copy.RestoreRights = obj.RestoreRights || obj.restoreRights || CustomPermissions.None;
        copy.SecurityRights = obj.SecurityRights || obj.securityRights || CustomPermissions.None;
        return copy;
    }
}