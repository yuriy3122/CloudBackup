import { Role } from "./Role";
import { Tenant } from './Tenant';
import { UtcOffset } from "./UtcOffset";

export class User {
    public Id: number;
    public RowVersion: string;
    public Name: string;
    public Description: string;
    public Login: string;
    public Password: string;
    public Email: string;
    public IsEnabled: boolean;
    public UtcOffset: UtcOffset;
    public Tenant: Tenant;
    public Role: Role;

    public CanDelete: boolean = true;
    public CantDeleteReason: string;

    constructor(id?: number) {
        this.Id = id != null ? id : 0;
        this.RowVersion = this.Name = this.Description = this.Login = this.Email = this.Password = this.CantDeleteReason = "";
        this.IsEnabled = true;
    }

    static Copy(obj: any): User {
        let copy = new User(obj.Id || obj.id);
        copy.RowVersion = obj.RowVersion || obj.rowVersion || "";
        copy.Name = obj.Name || obj.name || "";
        copy.Description = obj.Description || obj.description || "";
        copy.Login = obj.Login || obj.login || "";
        copy.Password = obj.Password || obj.password || "";
        copy.Email = obj.Email || obj.email || "";
        copy.IsEnabled = obj.IsEnabled || obj.isEnabled;

        copy.CanDelete = obj.CanDelete || obj.canDelete;
        copy.CantDeleteReason = obj.CantDeleteReason || obj.cantDeleteReason || "";

        let offset = obj.UtcOffset || obj.utcOffset;
        copy.UtcOffset = UtcOffset.Copy(offset);

        let tenant = obj.Tenant || obj.tenant;
        copy.Tenant = Tenant.Copy(tenant);

        let role = obj.Role || obj.role;
        copy.Role = Role.Copy(role);

        return copy;
    }
}
