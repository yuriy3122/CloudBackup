//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, DoCheck, ViewChild, ChangeDetectorRef } from '@angular/core';
import { DxDataGridComponent } from 'devextreme-angular';
//services
import { NotificationService } from '../../../services/common/NotificationService';
import { BackupService } from '../../../services/backup/BackupService';
//classes
import { RestoreOptions, InstanceDiskRestoreOptions } from '../../../classes/RestoreOptions';
import { Disk } from "../../../classes/Disk";
//Enums
import { InstanceLaunchMode, AmiHandlingMode } from "../../../classes/Enums";
import { diskColumns } from 'src/app/classes/constants';

@Component({
    selector: 'restore-job-instance-options-dialog',
    templateUrl: './RestoreJobInstanceOptionsDialog.template.html'
})

@Injectable()
export class RestoreJobInstanceOptionsDialog implements OnInit, DoCheck {
    title: string = "Restore Options For Instances";

    public selectedIndex: number = 0; // selected instance index
    public instanceOptionTabs: { id: number, text: string }[];
    public selectedInstanceOptionTab: { id: number, text: string };

    private isKeysLoading: boolean = false;
    private isVpcsLoading: boolean = false;
    private isSubnetsLoading: boolean = false;
    private isSecurityGroupsLoading: boolean = false;
    private isInstanceTypesLoading: boolean = false;
    private isDisksLoading: boolean = false;
    private isIamRolesLoading: boolean = false;
    private isAmiInfoLoading: boolean = false;
    private isAvailabilityZonesLoading: boolean = false;
    private isLaunchModesLoading: boolean = false;

    @Input()
    public popupVisible: boolean;
    @Output()
    popupVisibleChange = new EventEmitter<boolean>();

    @Input() backupIds: number[] = [];

    @Input()
    public instances: Array<any> = [];

    @Input()
    public instanceOptions: Array<RestoreOptions> = [];

    public instanceOptionsStore: Array<RestoreOptions> = [];

    @Output()
    instanceOptionsChange = new EventEmitter<any>();

    // disks
    disks: Disk[] = [];

    @ViewChild("disksGrid") disksGrid: DxDataGridComponent;
    selectedDiskRows: Disk[];
    disksColumns = diskColumns;

    get isLoading(): boolean {
        return this.isKeysLoading ||
            this.isVpcsLoading ||
            this.isSubnetsLoading ||
            this.isSecurityGroupsLoading ||
            this.isInstanceTypesLoading ||
            this.isDisksLoading ||
            this.isIamRolesLoading ||
            this.isAmiInfoLoading ||
            this.isAvailabilityZonesLoading ||
            this.isLaunchModesLoading;
    }

    get syncInstanceCount(): number {
        if (this.instanceOptionsStore.length > this.selectedIndex) {
            return this.instanceOptionsStore[this.selectedIndex].InstanceCount;
        }
        else {
            return 1;
        }
    }

    set syncInstanceCount(value: number) {
        if (this.instanceOptionsStore.length > this.selectedIndex) {
            this.instanceOptionsStore[this.selectedIndex].InstanceCount = value;
        }
    }

    get syncInstancePassword(): string {
        if (this.instanceOptionsStore.length > this.selectedIndex) {
            return this.instanceOptionsStore[this.selectedIndex].InstancePassword;
        }
        else {
            return "";
        }
    }

    set syncInstancePassword(value: string) {
        if (this.instanceOptionsStore.length > this.selectedIndex) {
            this.instanceOptionsStore[this.selectedIndex].InstancePassword = value;
        }
    }

    constructor(private backupService: BackupService, private notificator: NotificationService) {
    }

    onShow() {
        this.selectedIndex = 0;
        this.disks = [];

        this.instanceOptionsStore = [];
        for (var instance of this.instances) {
            for (var instanceOptions of this.instanceOptions) {
                if (instanceOptions.InstanceId === instance.id) {
                    let copy = RestoreOptions.Copy(instanceOptions);
                    this.instanceOptionsStore.push(copy);
                }
            }
        }

        // this.setAvailableLaunchModes().then(() => this.selectInstance(0));
        this.selectedInstanceOptionTab = this.instanceOptionTabs[0];
        this.popupVisibleChange.emit(this.popupVisible);
    }

    save = () => {
        for (var instanceOptionsStore of this.instanceOptionsStore) {
            for (var i = 0; i < this.instanceOptions.length; i++) {
                if (instanceOptionsStore.InstanceId === this.instanceOptions[i].InstanceId) {
                    this.instanceOptions[i] = RestoreOptions.Copy(instanceOptionsStore);
                }
            }
        }
        this.instanceOptionsChange.emit(this.instanceOptions);
        this.hide();
    }

    ngDoCheck(): void {
        this.popupVisibleChange.emit(this.popupVisible);
        this.instanceOptionsChange.emit(this.instanceOptions);
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
        this.instanceOptionTabs = [
            {
                id: 0,
                text: "Instance Options",
            }
        ];

        this.selectedInstanceOptionTab = this.instanceOptionTabs[0];
    }

    selectInstance(index: number) {
        this.selectedIndex = index;
        this.setDisks();
    }

    private setDisks() {
        this.isDisksLoading = true;

        const disksPromise = this.backupIds && this.backupIds.length > 0
            ? this.backupService.getBackupObjectDisksList(this.backupIds, this.instances[this.selectedIndex].instanceId)
            : Promise.resolve(new Array<Disk>());

        const options = this.instanceOptionsStore[this.selectedIndex];

        disksPromise
            .then(disks => {
                this.disks = disks;
                this.selectedDiskRows = this.disks.filter(disk => {
                    const diskOptions = options.InstanceDiskRestoreOptions.find(x => x.DiskId === disk.id);
                    return diskOptions == null || !diskOptions.Exclude;
                });
                this.isDisksLoading = false;
            })
            .catch(() => {
                this.disks = this.selectedDiskRows = [];
                this.isDisksLoading = false;
            });
    }

    onSelectedDisksChanged() {
        if (this.instanceOptionsStore.length > this.selectedIndex) {
            this.instanceOptionsStore[this.selectedIndex].InstanceDiskRestoreOptions =
                this.disks.map(disk => {
                    const diskOptions = new InstanceDiskRestoreOptions();
                    diskOptions.DiskId = disk.id;
                    diskOptions.Exclude = !this.selectedDiskRows.some(x => x.id === disk.id);
                    return diskOptions;
                });
        }
    }

}
