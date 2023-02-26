//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, DoCheck } from '@angular/core';
//services
import { NotificationService } from '../../../services/common/NotificationService';
import { BackupService } from '../../../services/backup/BackupService';
//classes
import { RestoreOptions } from '../../../classes/RestoreOptions';
import { DiskRestoreMode } from "../../../classes/Enums";

@Component({
    selector: 'restore-job-disk-options-dialog',
    templateUrl: './RestoreJobDiskOptionsDialog.template.html'
})

@Injectable()
export class RestoreJobDiskOptionsDialog implements OnInit, DoCheck {
    title: string = "Restore Options For Disks";

    public selectedIndex: number = 0; // selected instance index
    public diskOptionTabs: { id: number, text: string }[];
    public selectedDiskOptionTab: { id: number, text: string };

    private isAvailabilityZonesLoading: boolean = false;
    private isInstancesLoading: boolean = false;
    private isDevicesLoading: boolean = false;
    private isDiskTypesLoading: boolean = false;
    private showInstances: boolean = false;
    public showDeviceInput: boolean = false;
    private showDeviceSelection: boolean = false;

    public instanceSelectionValid: boolean = true;
    public deviceSelectionValid: boolean = true;
    public minDiskSize: number = 1;
    public maxDiskSize: number = 16384;

    private diskRestoreModes = [
        { name: "Standalone disk", value: 1 },
        { name: "Attach to instance (only if device is free)", value: 2 },
        { name: "Switch attached disk", value: 3 },
        { name: "Switch attached disk and delete old", value: 4 },
    ];

    private availabilityZones: { name: string }[] = [];
    private instances: { id: string, name: string }[] = [];
    private devices: { device: string }[] = [];
    private diskTypes: any[] = [];

    @Input()
    public popupVisible: boolean;
    @Output()
    popupVisibleChange = new EventEmitter<boolean>();

    @Input() backupIds: number[] = [];

    @Input()
    public disks: Array<any> = [];

    @Input()
    public diskOptions: Array<RestoreOptions> = [];
    
    @Output()
    diskOptionsChange = new EventEmitter<any>();

    public diskOptionsStore: Array<RestoreOptions> = [];

    get isLoading(): boolean {
        return this.isAvailabilityZonesLoading ||
            this.isInstancesLoading ||
            this.isDevicesLoading ||
            this.isDiskTypesLoading;
    }

    constructor(private backupService: BackupService, private notificator: NotificationService) {
    }

    onShow() {
        this.selectedIndex = 0;
        this.diskOptionsStore = [];
        this.showInstances = false;
        this.showDeviceSelection = false;
        this.showDeviceInput = false;
        this.instanceSelectionValid = true;
        this.deviceSelectionValid = true;

        for (var disk of this.disks) {
            for (var diskOptions of this.diskOptions) {
                if (diskOptions.DiskId === disk.id) {
                    let copy = RestoreOptions.Copy(diskOptions);
                    this.diskOptionsStore.push(copy);
                }
            }
        }

        this.selectDisk(0);
        this.selectedDiskOptionTab = this.diskOptionTabs[0];
        this.popupVisibleChange.emit(this.popupVisible);
    }

    save = () => {
        if (!this.validate()) {
            return;
        }

        for (var diskOptionsStore of this.diskOptionsStore) {
            for (var i = 0; i < this.diskOptions.length; i++) {
                if (diskOptionsStore.DiskId === this.diskOptions[i].DiskId) {
                    this.diskOptions[i] = RestoreOptions.Copy(diskOptionsStore);
                }
            }
        }

        this.diskOptionsChange.emit(this.diskOptions);
        this.hide();
    }

    ngDoCheck(): void {
        this.popupVisibleChange.emit(this.popupVisible);
    }

    onHiding() {
        this.popupVisible = false;
        this.popupVisibleChange.emit(false);
    }

    hide() {
        this.popupVisible = false;
        this.popupVisibleChange.emit(false);
    }

    ngOnInit(): void {
        this.diskOptionTabs = [
            {
                id: 0,
                text: "Basic",
            }
        ];

        this.selectedDiskOptionTab = this.diskOptionTabs[0];
    }

    get syncDiskRestoreMode(): DiskRestoreMode {
        if (this.diskOptionsStore.length > this.selectedIndex) {
            return this.diskOptionsStore[this.selectedIndex].DiskRestoreMode;
        }
        else {
            return DiskRestoreMode.Standalone;
        }
    }

    set syncDiskRestoreMode(value: DiskRestoreMode) {
        if (this.diskOptionsStore.length > this.selectedIndex) {
            this.diskOptionsStore[this.selectedIndex].DiskRestoreMode = value;
            this.updateControls(value);
        }
    }

    setDevicesForInstance(instanceId: string) {
        this.devices = [];

        if (!instanceId) {
            return;
        }

        let index = this.selectedIndex;

        //todo: remove magic numbers
        if (this.disks.length > index && this.diskOptionsStore.length > index) {
            if (this.diskOptionsStore[index].DiskRestoreMode != 3 &&
                this.diskOptionsStore[index].DiskRestoreMode != 4) {
                return;
            }
        }
    }


    selectDisk(index: number) {
        this.selectedIndex = index;

        if (this.disks.length > index && this.diskOptionsStore.length > index) {
            if (!this.diskOptionsStore[index].DiskRestoreMode) {
                this.diskOptionsStore[index].DiskRestoreMode = DiskRestoreMode.Standalone;
            }

            let restoreMode = this.diskOptionsStore[index].DiskRestoreMode;
            this.updateControls(restoreMode);
        }

        this.setDiskCapacityLimits();
    }

    setDiskCapacityLimits() {
        for (var i = 0; i < this.disks.length; i++) {
            if (this.diskOptionsStore[this.selectedIndex].DiskId == this.disks[i].diskId) {
                this.minDiskSize = this.disks[i].storageSize;

                let size = this.diskTypes[i].diskMinSize;

                if (this.minDiskSize < size) {
                    this.minDiskSize = size;
                }
            }
        }
    }

    updateControls(value: DiskRestoreMode) {
        if (value == DiskRestoreMode.Standalone) {
            this.showInstances = false;
            this.showDeviceInput = false;
            this.showDeviceSelection = false;
        }
        else if (value == DiskRestoreMode.AttachToInstanceOnlyIfDeviceIsFree) {
            this.showInstances = true;
            this.showDeviceInput = true;
            this.showDeviceSelection = false;
        }
        else {
            this.showInstances = true;
            this.showDeviceInput = false;
            this.showDeviceSelection = true;
        }

        let index = this.selectedIndex;

        if (this.showDeviceSelection && this.disks.length > index && this.diskOptionsStore.length > index) {
            let instanceId = this.diskOptionsStore[this.selectedIndex].InstanceId;

            if (instanceId != null) {
                this.setDevicesForInstance(instanceId);
            }
        }
    }

    private validate(): boolean {
        this.instanceSelectionValid = true;
        this.deviceSelectionValid = true;

        for (var i = 0; i < this.diskOptionsStore.length; i++) {
            let instanceAndDeviceCheck = this.diskOptionsStore[i].DiskRestoreMode == DiskRestoreMode.SwitchAttachedDisk ||
                this.diskOptionsStore[i].DiskRestoreMode == DiskRestoreMode.AttachToInstanceOnlyIfDeviceIsFree ||
                this.diskOptionsStore[i].DiskRestoreMode == DiskRestoreMode.SwitchAttachedDiskAndDeleteOld;

            if (instanceAndDeviceCheck && !this.diskOptionsStore[i].InstanceId) {
                this.instanceSelectionValid = false;
                this.selectDisk(i);

                return false;
            }

            if (instanceAndDeviceCheck && !this.diskOptionsStore[i].DiskId) {
                this.deviceSelectionValid = false;
                this.selectDisk(i);

                return false;
            }
        }

        return true;
    }
}
