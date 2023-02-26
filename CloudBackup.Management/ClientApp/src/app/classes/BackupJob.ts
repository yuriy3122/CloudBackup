import { Schedule } from './Schedule';
import { JobObject } from './JobObject';

export class BackupJob {
    public Id: number;
    public UserId: number;
    public RowVersion?: string;
    public Name: string;
    public Type: string;
    public Description: string;
    public LastRunAt?: string;
    public Status: string;
    public Schedule: Schedule;
    public JobOptions: string;
    public JobObjects: Array<JobObject>;
    public ObjectCount: number;
    public TenantId: number;
    public TenantName?: string;

    constructor(id?: number, userId?: number) {
        this.Id = id != null ? id : 0;
        this.UserId = userId != null ? userId : 0;
        this.ObjectCount = 0;
        this.RowVersion = this.Name = this.Description = this.LastRunAt = this.Status = this.Name = this.TenantName = "";
        this.Schedule = new Schedule();
        this.JobOptions = "";
        this.JobObjects = [];
        this.TenantId = 0;
    }

    public get isNameValid(): boolean {
        return this.Name != null && this.Name.length > 0;
    }

    static Copy(obj: any): BackupJob {
        let copy = new BackupJob(obj.Id || obj.id, obj.UserId || obj.userId);
        copy.RowVersion = obj.RowVersion || obj.rowVersion || "";
        copy.Name = obj.Name || obj.name || "";
        copy.Description = obj.Description || obj.description || "";
        copy.LastRunAt = obj.LastRunAt || obj.lastRunAt || "";
        copy.Status = obj.Status || obj.status || "";
        copy.ObjectCount = obj.ObjectCount || obj.objectCount || 0;
        copy.Schedule = obj.Schedule == null ? (obj.schedule == null ? new Schedule() : Schedule.Copy(obj.schedule)) : Schedule.Copy(obj.Schedule);
        copy.TenantId = obj.TenantId || obj.tenantId || 0;
        copy.TenantName = obj.TenantName || obj.tenantName || "";
        copy.JobOptions = obj.jobOptions || obj.JobOptions || "";
        
        //Job Objects
        let jobObjects = new Array<any>();
        if (Array.isArray(obj.jobObjects))
            jobObjects = obj.jobObjects;
        if (Array.isArray(obj.JobObjects))
            jobObjects = obj.JobObjects;
        copy.JobObjects = jobObjects.map(x => JobObject.Copy(x));

        return copy;
    }
}