export class Role {
    public Id: number;//hidden field
    public RowVersion: string;//hidden field
    public Name: string;
    public IsGlobalAdmin: boolean;

    constructor(id?: number) {
        this.Id = id != null ? id : 0;
        this.RowVersion = this.Name = "";
        this.IsGlobalAdmin = false;
    }

    static Copy(obj: any): Role {
        if (obj == null)
            return new Role(0);
        let copy = new Role(obj.Id || obj.id);
        copy.RowVersion = obj.RowVersion || obj.rowVersion || "";
        copy.Name = obj.Name || obj.name || "";
        copy.IsGlobalAdmin = obj.IsGlobalAdmin || obj.isGlobalAdmin || false;
        return copy;
    }
}