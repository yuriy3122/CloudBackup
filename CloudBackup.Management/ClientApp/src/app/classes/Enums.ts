export enum DayOfWeekBin {
    NoDays = 0,
    Monday = 2,
    Tuesday = 4,
    Wednesday = 8,
    Thursday = 16,
    Friday = 32,
    Saturday = 64,
    Sunday = 1
}

export enum DayOfWeek {
    Sunday = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6
}

export enum FilterDates {
    Day = 0,
    Week = 1,
    Month = 2,
    LastMonth = 3
};

export enum DateType {
    Range = 1,
    Custom = 2
};

export enum YesNo {
    No = 0,
    Yes = 1
};

export enum JobObjectType {
    Instance = 0,
    Disk = 1,
    RDS = 2,
    AuroraCluster = 3,
    RedshiftCluster = 4,
    Dynamo = 5,
    HyperV = 6,
    ESXi = 7,
    FSx = 10
};

export enum SchedulerType {
    Immediately = 0,
    Delayed = 1,
    Periodically = 2,
    Daily = 3,
    Monthly = 4,
}

export enum StartupType {
    Immediately = 0,
    Delayed = 1,
    Recurring = 2
}

export enum OccurType {
    Unknown = -1,
    Daily = 0,
    Periodically = 1,
    Monthly = 2
}

export enum BackupObjectStatus {
    Success = 0,
    Failed = 1,
    Warning = 2,
    Running = 3
};

export enum UserRole { NoAccess = 0, ITAdministrator = 1, SecurityAdministrator = 2, Auditor = 4 }


export enum AuditOperation {
    TestMethod = 10000000,
    None = 0,
};

export class AuditOperationDict {
    static auditOperations: { [unit: number]: string } = {
        10000000: "Test Method",
        0: "None"
    };

    static Get(id: number) {
        return this.auditOperations[id];
    }

    static GetAuditOperation(id: AuditOperation) {
        return this.auditOperations[id];
    }
}

export enum DiskRestoreMode {
    //Disk attached to newly created instance
    Default = 0,

    //Standalone disk restore
    Standalone = 1,

    //Attach disk only if selected device if free, otherwise throw exception
    AttachToInstanceOnlyIfDeviceIsFree = 2,

    //Switch attached disk. Instance must be in stopped state before attachment
    SwitchAttachedDisk = 3,

    //Switch attached disk, delete old disk after new disk attached. Instance must be in stopped state before attachment
    SwitchAttachedDiskAndDeleteOld = 4
}

export enum InstanceLaunchMode {
    //Register new AMI from existing snapshot(Linux Instances)
    LaunchFromSnapshot = 0,

    //Launch from existing AMI (Windows Instances)
    LaunchFromAmi = 1
}

export enum AmiHandlingMode {
    LeaveRegisteredAfterRecovery = 0,

    DeRegisterAfterRecovery = 1
}

export enum VMType {
    HyperV = 0,
    VMWare = 1
}

export enum EC2ResourceType {
    Instance = 0,
    Disk = 1
}

export enum NotificationType {
  Alert = 0,
  DailySummary = 1
}

export enum DeliveryMethod {
  Smtp = 0,
  Sms = 1
}
