import { Tenant } from './Tenant';
import { User } from './User';

export enum ProfileState {
    Invalid,
    Valid
}

export enum AuthenticationType {
    IAMUser = 0,
    InstanceIAMRole = 1,
    AssumeRole = 2
}

export class Profile {
    public Id: number;
    public RowVersion: string;
    public Name: string;
    public Description: string;
    public ServiceAccountId: string;
    public KeyId: string;
    public PrivateKey: string;
    public Created!: Date;
    public CreatedText!: string;
    public State!: ProfileState;
    public IsSystem!: boolean;
    public Owner!: User | null;
    public Tenant!: Tenant | null;
    public AuthenticationType: AuthenticationType;
    public UsedInJobs = false;

    constructor(id?: number) {
        this.Id = id != null ? id : 0;
        this.AuthenticationType = AuthenticationType.AssumeRole;
        this.RowVersion = this.Name = this.Description = this.ServiceAccountId = this.PrivateKey = this.KeyId = "";
    }

    static Copy(obj: any): Profile {
        let copy = new Profile(obj.Id || obj.id);
        copy.RowVersion = obj.RowVersion || obj.rowVersion || "";
        copy.Name = obj.Name || obj.name || "";
        copy.Description = obj.Description || obj.description || "";
        copy.ServiceAccountId = obj.ServiceAccountId || obj.serviceAccountId || "";
        copy.KeyId = obj.KeyId || obj.keyId || "";
        copy.PrivateKey = obj.PrivateKey || obj.privateKey || "";
        copy.Created = obj.Created || obj.created;
        copy.CreatedText = obj.CreatedText || obj.createdText || "";
        copy.State = obj.State || obj.state;
        copy.UsedInJobs = obj.UsedInJobs || obj.usedInJobs;
        copy.AuthenticationType = obj.AuthenticationType || obj.authenticationType;

        let tenant = obj.Tenant || obj.tenant;
        copy.Tenant = tenant == null ? null : Tenant.Copy(tenant);

        let owner = obj.Owner || obj.owner;
        copy.Owner = owner == null ? null : User.Copy(owner);

        return copy;
    }
}
