import { RetentionPolicy } from "./RetentionPolicy";
import { ReplicationOptions } from "./ReplicationOptions";

export enum BackupAfterErrorStatus {
    Failed = 0,
    Warning = 1
}

export enum InstanceBackupMode {
    Default = 0,
    AmiOnly = 1
}

export enum TagJoinType {
    And = 0,
    Or = 1
}

export class JobOptions {
    AppConsistent = false;
    NoReboot = false;
    RetentionPolicy: RetentionPolicy;
    ReplicationOptions: ReplicationOptions;

    StatusAfterScriptError!: BackupAfterErrorStatus;
    StatusAfterReplicationError!: BackupAfterErrorStatus;

    InstanceBackupMode!: InstanceBackupMode;
    TagJoinType!: TagJoinType;

    constructor(retentionPolicy: RetentionPolicy, replicationOptions: ReplicationOptions) {
        this.RetentionPolicy = retentionPolicy;
        this.ReplicationOptions = replicationOptions;
    }

    static Copy(obj: any): JobOptions {
        var policy = RetentionPolicy.Copy(obj.RetentionPolicy || obj.retentionPolicy);
        var replication = ReplicationOptions.Copy(obj.ReplicationOptions || obj.replicationOptions);
        let copy = new JobOptions(policy, replication);
        copy.AppConsistent = obj.appConsistent || obj.AppConsistent || false; // for backwards compatibility
        copy.NoReboot = obj.noReboot || obj.NoReboot || false; // for backwards compatibility

        copy.StatusAfterScriptError = obj.statusAfterScriptError || obj.StatusAfterScriptError;
        copy.StatusAfterReplicationError = obj.StatusAfterReplicationError || obj.StatusAfterReplicationError;

        copy.InstanceBackupMode = obj.InstanceBackupMode || obj.InstanceBackupMode;
        copy.TagJoinType = obj.TagJoinType;

        return copy;
    }
}
