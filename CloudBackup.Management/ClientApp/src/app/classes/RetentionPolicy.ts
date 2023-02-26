
export class RetentionPolicy {
    public RestorePointsToKeep: number;
    public TimeIntervalValue: number;
    public RetentionTimeIntervalType: number;
    public RetentionAction: number;

    constructor(restorePointsToKeep: number, timeIntervalValue: number, retentionTimeIntervalType: number, retentionAction: number) {
        if (retentionAction === 0) {
            this.RestorePointsToKeep = restorePointsToKeep || 1;
        }
        else {
            this.RestorePointsToKeep = restorePointsToKeep;
        }

        this.TimeIntervalValue = timeIntervalValue || 1;
        this.RetentionTimeIntervalType = retentionTimeIntervalType;
        this.RetentionAction = retentionAction;
    }

    static Copy(obj: any): RetentionPolicy {
        let copy = new RetentionPolicy(obj.restorePointsToKeep || obj.RestorePointsToKeep,
            obj.timeIntervalValue || obj.TimeIntervalValue,
            obj.retentionTimeIntervalType || obj.RetentionTimeIntervalType,
            obj.retentionAction || obj.RetentionAction);

        return copy;
    }
}