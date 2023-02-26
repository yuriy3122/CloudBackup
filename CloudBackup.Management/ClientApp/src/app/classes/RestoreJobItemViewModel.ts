import { RestoreOptions } from './RestoreOptions';

export class RestoreJobItemViewModel {
    public RestoreOptions: RestoreOptions;
    public InstanceId: string;
    public DiskId: string;

    constructor(restoreOptions: RestoreOptions, instanceId: string, diskId: string, dbInstanceArn: string, clusterArn: string, redshiftClusterArn: string, vmId: string) {
        this.RestoreOptions = restoreOptions;
        this.InstanceId = instanceId;
        this.DiskId = diskId;
    }
}