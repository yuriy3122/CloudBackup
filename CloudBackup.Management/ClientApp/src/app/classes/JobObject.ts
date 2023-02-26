import { JobObjectType } from './Enums';

export class JobObject {
    public Id: number;
    public ProfileId: number;
    public ParentId: string;//hidden field
    public ObjectId: string;
    public Type: JobObjectType;
    public FolderId: string;

    constructor(id?: number, profileId?: number) {
        this.Id = id != null ? id : 0;
        this.ProfileId = profileId != null ? profileId : 0;
        this.ParentId = this.ObjectId = this.FolderId = "";
        this.Type = JobObjectType.Instance;
    }

    static Copy(obj: any): JobObject {
        let copy = new JobObject(obj.Id || obj.id, obj.ProfileId || obj.profileId);
        copy.ParentId = obj.ParentId || obj.parentId || "";
        copy.ObjectId = obj.ObjectId || obj.objectId || "";
        copy.Type = obj.Type || obj.type || JobObjectType.Instance;
        copy.FolderId = obj.FolderId || obj.folderId || "";
        
        return copy;
    }

    static FromInstance(obj: any) {
        let copy = new JobObject();
        copy.ObjectId = obj.Id || obj.id;
        copy.ProfileId = obj.ProfileId || obj.profileId;
        copy.FolderId = obj.FolderId || obj.folderId;
        copy.Type = JobObjectType.Instance;
        return copy;
    }

    static FromDisk(obj: any) {
        let copy = new JobObject();
        copy.ObjectId = obj.Id || obj.id;
        copy.ProfileId = obj.ProfileId || obj.profileId;
        copy.FolderId = obj.FolderId || obj.folderId;
        copy.ParentId = obj.InstanceId || obj.instanceId || "";
        copy.Type = JobObjectType.Disk;
        return copy;
    }
}