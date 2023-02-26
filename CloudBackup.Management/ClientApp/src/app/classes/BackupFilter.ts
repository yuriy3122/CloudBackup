export enum BackupStatus {
    Success,
    Failed,
    Warning,
    Running,
    Replicating
};

export interface BackupFilter {
    minFinishDate?: any;
    maxFinishDate?: any;
    status?: BackupStatus;
    onlyPermanent?: any;
    restoreEligible?: any;
}