import { DiskRestoreMode, InstanceLaunchMode, AmiHandlingMode } from "./Enums";
import { Helper } from "../helpers/Helper";

export class RestoreOptions {
    public DestPlacement: string;
    public DestProfileId: number;
    // instance
    public InstanceCount: number;
    public InstanceId: string;
    public InstancePassword: string;
    public InstanceDiskRestoreOptions: InstanceDiskRestoreOptions[];
    // disk
    public DiskRestoreMode: DiskRestoreMode;
    public DiskId: string;
    public FolderId: string;

    constructor(placement: string, profileId: number) {
        this.InstanceId = "";
        this.InstanceCount = 1;
        this.InstancePassword = "";
        this.DestProfileId = profileId || 0;
        this.DestPlacement = placement || "";
        this.InstanceDiskRestoreOptions = [];
        this.DiskRestoreMode = 0;
        this.DiskId = "";
        this.FolderId = "";
    }

    static Copy(obj: any): RestoreOptions {
        let copy = new RestoreOptions(obj.placement || obj.Placement, obj.profileId || obj.ProfileId);
        copy.InstanceId = Helper.Coalesce(obj.InstanceId, obj.instanceId, "");
        copy.InstanceCount = Helper.Coalesce(obj.InstanceCount, obj.instanceCount, 1);
        copy.InstancePassword = Helper.Coalesce(obj.InstancePassword, obj.instancePassword, "");
        copy.DestPlacement = Helper.Coalesce(obj.DestPlacement, obj.destPlacement, "");
        copy.DestProfileId = Helper.Coalesce(obj.DestProfileId, obj.destProfileId, 0);

        const diskRestoreOptions = Helper.Coalesce(obj.InstanceDiskRestoreOptions, obj.instanceDiskRestoreOptions, []);
        copy.InstanceDiskRestoreOptions = [].concat(diskRestoreOptions);

        copy.DiskId = Helper.Coalesce(obj.diskId, obj.DiskId, "");
        copy.DiskRestoreMode = Helper.Coalesce(obj.diskRestoreMode, obj.DiskRestoreMode, 0);
        copy.FolderId = Helper.Coalesce(obj.folderId, obj.FolderId);

        return copy;
    }
}

export class InstanceDiskRestoreOptions {
    DiskId: string;
    Exclude: boolean = false;
}
