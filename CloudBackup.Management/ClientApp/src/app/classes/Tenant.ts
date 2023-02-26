import { User } from './User';

export class Tenant {

    public Id: number; // hidden field
    public RowVersion: string;
    public Name: string;
    public Description: string;
    public Admins: User[];

    public Isolated = false;
    public IsSystem = false;

    public CanEdit = true;
    public CantEditReason: string;
    public CanDelete = true;
    public CantDeleteReason: string;

    constructor(id?: number) {
        this.Id = id || 0;
        this.Name = this.Description = this.RowVersion = this.CantEditReason = this.CantDeleteReason = "";
        this.Admins = [];
    }

    static Copy(obj: any): Tenant {
        if (obj == null)
            return new Tenant(0);

        let copy = new Tenant(obj.Id || obj.id);
        copy.RowVersion = obj.RowVersion || obj.rowVersion || "";
        copy.Name = obj.Name || obj.name || "";
        copy.Description = obj.Description || obj.description || "";
        copy.IsSystem = obj.IsSystem || obj.isSystem;
        copy.Isolated = obj.Isolated || obj.isolated;

        copy.Admins = [];
        let admins = obj.Admins || obj.admins;
        if (admins != null && Array.isArray(admins)) {
            for (var i = 0; i < admins.length; i++)
                copy.Admins.push(User.Copy(admins[i]));
        }

        copy.CanEdit = obj.CanEdit || obj.canEdit;
        copy.CantEditReason = obj.CantEditReason || obj.cantEditReason || "";
        copy.CanDelete = obj.CanDelete || obj.canDelete;
        copy.CantDeleteReason = obj.CantDeleteReason || obj.cantDeleteReason || "";

        return copy;
    }
}