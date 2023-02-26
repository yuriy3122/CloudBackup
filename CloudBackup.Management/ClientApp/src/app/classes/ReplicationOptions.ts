export class ReplicationOptions {
    public EnableReplication: boolean;
    public BackupInverval: number;
    public ReplicationRegions: Array<string>;
    public EnableCrossAccountReplication: boolean;
    public ReplicationAccountId: number;
    public EnableCrossCloudReplication: boolean;
    public AzureBlobStorageAccountId: number;

    constructor(enableReplication: boolean, backupInverval: number, enableCrossAccountReplication: boolean, replicationAccountId: number) {
        this.EnableReplication = enableReplication;
        this.BackupInverval = backupInverval;
        this.ReplicationRegions = [];
        this.EnableCrossAccountReplication = enableCrossAccountReplication;
        this.ReplicationAccountId = replicationAccountId;
        this.AzureBlobStorageAccountId = 0;
        this.EnableCrossCloudReplication = false;
    }

    static Copy(obj: any): ReplicationOptions {
        let copy = new ReplicationOptions(obj.enableReplication || obj.EnableReplication,
            obj.backupInverval || obj.BackupInverval,
            obj.enableCrossAccountReplication || obj.EnableCrossAccountReplication,
            obj.replicationAccountId || obj.ReplicationAccountId);

        copy.EnableCrossCloudReplication = obj.enableCrossCloudReplication || obj.EnableCrossCloudReplication;
        copy.AzureBlobStorageAccountId = obj.azureBlobStorageAccountId || obj.AzureBlobStorageAccountId;

        if (obj.ReplicationRegions != null && Array.isArray(obj.ReplicationRegions)) {
            for (var i = 0; i < obj.ReplicationRegions.length; i++)
                copy.ReplicationRegions.push(obj.ReplicationRegions[i]);
        }

        if (obj.replicationRegions != null && Array.isArray(obj.replicationRegions)) {
            for (var i = 0; i < obj.replicationRegions.length; i++)
                copy.ReplicationRegions.push(obj.replicationRegions[i]);
        }

        return copy;
    }
}