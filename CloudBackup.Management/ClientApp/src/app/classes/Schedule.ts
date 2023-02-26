import { OccurType, StartupType } from './Enums';
import { Tenant } from "./Tenant";

export class Schedule {
    public Id: number;//hidden field
    public RowVersion: string;//hidden field
    public Name: string;
    public TenantId: number;
    public Tenant: Tenant | null;
    public StartupType: StartupType;
    public OccurType: OccurType;
    public Created: Date;
    public JobCount: number;
    public Params: string;
    public ParamsDescription: string;

    constructor(id?: number) {
        this.Id = id != null ? id : 0;
        this.RowVersion = this.Name = this.Params = this.ParamsDescription = "";
        this.StartupType = StartupType.Recurring;
        this.OccurType = OccurType.Daily;
        this.TenantId = 0;
        this.JobCount = 0;
        this.Created = new Date(0, 0, 0);
        this.Tenant = null;
    }

    static Copy(obj: any): Schedule {
        let copy = new Schedule(obj.Id || obj.id);
        copy.RowVersion = obj.RowVersion || obj.rowVersion || "";
        copy.Name = obj.Name || obj.name || "";
        copy.TenantId = obj.TenantId || obj.tenantId || 0;
        copy.StartupType = obj.StartupType || obj.startupType || 0;
        copy.OccurType = obj.OccurType || obj.occurType || 0;
        copy.Created = obj.Created || obj.created;
        copy.JobCount = obj.JobCount || obj.jobCount || 0;
        copy.Params = obj.Params || obj.params || "";
        copy.ParamsDescription = obj.ParamsDescription || obj.paramsDescription || "";

        let tenant = obj.Tenant || obj.tenant;
        copy.Tenant = tenant == null ? null : Tenant.Copy(tenant);

        return copy;
    }
}