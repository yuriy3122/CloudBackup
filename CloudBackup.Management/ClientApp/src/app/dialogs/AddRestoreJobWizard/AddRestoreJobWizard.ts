//inner
import { Component, Input, Output, OnInit, EventEmitter, DoCheck, ViewChild, ChangeDetectorRef } from '@angular/core';
import { DxDataGridComponent } from 'devextreme-angular';
//services
import { NotificationService } from '../../services/common/NotificationService';
import { BackupService } from '../../services/backup/BackupService';
import { AdministrationService } from "../../services/administration/AdministrationService";
//classes
import { WizardStep } from '../AddEditWizard/AddEditWizard';
import { RestoreJobWizardViewModel } from '../../classes/RestoreJobWizardViewModel';
import { RestoreJobItemViewModel } from '../../classes/RestoreJobItemViewModel';
import { RestoreOptions } from '../../classes/RestoreOptions';
import { JobObjectType, InstanceLaunchMode } from "../../classes/Enums";
import { Disk } from "../../classes/Disk";
import { Profile } from "../../classes/Profile";
import { Tenant } from "../../classes/Tenant";
//helper
import { Helper } from '../../helpers/Helper';
import { DevExtremeHelper } from "../../helpers/DevExtremeHelper";
import { BackupStatus } from 'src/app/classes/BackupFilter';
import { diskColumns, instanceColumns } from 'src/app/classes/constants';
import { DataGridSettings } from '../../classes';

export class RestoreJobWizardInitialObject {
    sourceObjectId: string;
    type: JobObjectType;

    constructor(sourceObjectId: string, type: JobObjectType) {
        this.sourceObjectId = sourceObjectId;
        this.type = type;
    }
}

export class RestoreJobWizardInitialData {
    backupIds: number[];
    sourceObjectIds: RestoreJobWizardInitialObject[];

    constructor(initialBackupIds: number[], initialSourceObjectIds?: RestoreJobWizardInitialObject[]) {
        this.backupIds = initialBackupIds || [];
        this.sourceObjectIds = initialSourceObjectIds || [];
    }
}

@Component({
    selector: 'add-restore-job-wizard',
    templateUrl: './AddRestoreJobWizard.template.html'
})
export class AddRestoreJobWizard implements OnInit, DoCheck {
    restoreJobObjectTabs: any;
    @Input() initialData: RestoreJobWizardInitialData;
    public gridsettings = new DataGridSettings();

    // backups step
    tenants: Array<Tenant> = [];
    selectedTenantId: number;
    selectedTenantIdChange = new EventEmitter<number>();
    selectedRestoreJobObjectTab: any;
    get canSelectTenants(): boolean {
        return this.tenants != null &&
            this.tenants.length > 1 &&
            (this.initialData == null || this.initialData.backupIds.length === 0);
    }

    backups: any = [];
    selectedBackups: Array<any>;
    get selectedBackupIds() {
        return this.selectedBackups ? this.selectedBackups.map(x => x.id) : [];
    }

    @ViewChild('selectedInstancesGrid')
    private selectedInstancesGrid: DxDataGridComponent;

    @ViewChild('selectedDisksGrid')
    private selectedDisksGrid: DxDataGridComponent;

    // restore mode step
    public restoreLocationMode: number;
    public restoreLocation: any;

    public regions: { name: string }[];
    public selectedRegion: string;

    public profiles: Profile[];
    public selectedProfileId: number;

    // objects step
    public restoreJobInstances: any[] = [];
    public restoreInstanceOptions: Array<RestoreOptions> = [];
    public restoreJobSelectedInstances: any[] = [];

    public restoreJobDisks: Disk[] = [];
    public restoreDiskOptions: Array<RestoreOptions> = [];
    public restoreJobSelectedDisks: Disk[] = [];

    private _showAttachedDisks: any = false;
    get showAttachedDisks() { return this._showAttachedDisks };
    set showAttachedDisks(value: any) {
        this._showAttachedDisks = value;
        this.setDisks();
    }

    public restoreJobInstancesLoading: boolean;
    public restoreJobDisksLoading: boolean;

    public validationMessageText: string;

    //Selected Objects for Summary
    public selectedObjects: { key: string, value: string }[] = [];

    public instanceOptionsDialogVisible: boolean = false;
    public diskOptionsDialogVisible: boolean = false;

    @Output()
    restoreJobCreated = new EventEmitter<any>();

    public backupsColumns = [
        { caption: "Job Name", dataField: "jobName" },
        { caption: "End Time", dataField: "finishedAt" },
        { caption: "Status", dataField: "status", cellTemplate: "backupStatusTemplate", width: "auto" },
      { caption: "Objects", dataField: "objectCount", width: "80" }
    ];

    public selectedInstanceColumns = instanceColumns;

    public selectedDiskColumns = diskColumns;

    public title: string;
    private clicked: boolean = false;
    private lock: boolean;
    public stepIndex: number;
    public steps: Array<WizardStep>;

    @Input()
    public popupVisible: boolean;

    @Output()
    public popupVisibleChange = new EventEmitter();

    constructor(private backupService: BackupService,
        private notificator: NotificationService,
        private administrationService: AdministrationService,
        private changeDetector: ChangeDetectorRef) {
    }

    ngDoCheck(): void {
        this.popupVisibleChange.emit(this.popupVisible);
    }

    ngOnInit(): void {
        this.stepIndex = 0;
        this.validationMessageText = "";

        this.steps = [
            new WizardStep(
                () => { },
                () => {
                    var result = this.validateSelectedBackups();
                    return result;
                },
                "Select backup",
                "Select backup to restore from"),
            // new WizardStep(
            //     () => { },
            //     () => true,
            //     "Restore mode",
            //     "Define restore mode"),
            new WizardStep(
                () => { },
                () => {
                    var result = this.validateJobObjects();
                    return result;
                },
                "Select objects",
                "Select objects to restore"),
            new WizardStep(
                () => { },
                () => true,
                "Summary",
                "Review job settings and press Finish button to run job")
        ];

        this.restoreJobObjectTabs = [];
        this.setObjectsTabs();
    }

    onShow() {
        this.stepIndex = 0;
        this.validationMessageText = "";
        this.setTenants()
            .then(() => this.setBackupsDataSource());

        this.selectedBackups = [];
        this.restoreJobInstancesLoading = false;
        this.restoreJobDisksLoading = false;
        this.restoreLocationMode = 0;
        this.selectedProfileId = 0;
        this.restoreJobSelectedInstances = [];
        this.restoreJobSelectedDisks = [];

        this.selectedObjects = [];

        this.title = "New Restore Job";

        this.restoreInstanceOptions = [];
        this.restoreDiskOptions = [];

        for (let i = 0; i < this.steps.length; i++)
            this.steps[i].IsValid = true;

        this.changeDetector.detectChanges();
    }

    onSearchValueChanged(grid: DxDataGridComponent, e: any) {
        grid.instance.searchByText(e.value);
    }

    onObjectSelectionChanged(e: any) {
        this.selectedObjects = [];

        for (var instance of this.restoreJobSelectedInstances) {
            this.selectedObjects.push({ key: instance.id, value: instance.name || instance.id });
        }
        for (var disk of this.restoreJobSelectedDisks) {
            this.selectedObjects.push({ key: disk.id, value: disk.name || disk.id });
        }

        this.validateJobWizardData();
    }

    onHiding = () => {
        this.popupVisible = false;
        this.popupVisibleChange.emit(false);
    }

    setStepIndex(step: number) {
        this.stepIndex = step || 0;
    }

    nextStep = () => {
        let stepResult = this.steps[this.stepIndex].OnNextStep();
        if (stepResult && this.steps[this.stepIndex].IsValid) {
            if (this.stepIndex < this.steps.length) {
                this.stepIndex++;
            }
            else {
                if (!this.clicked) {
                    this.onComplete();
                    this.clicked = true;
                }
            }
        }
    }

    prevStep = () => {
        if (this.stepIndex > 0)
            this.stepIndex--;
        else
            this.onHiding();
    }

    hide = () => {
        this.popupVisible = false;
        this.popupVisibleChange.emit(false);
    }

    onComplete = () => {
        this.validateSelectedBackups();
        this.validateJobWizardData();

        for (var i = 0; i < this.steps.length; i++) {
            if (!this.steps[i].IsValid) {
                this.stepIndex = i;
                return;
            }
        }

        var restoreJob = new RestoreJobWizardViewModel(this.selectedTenantId);
        restoreJob.SelectedBackupIds = this.selectedBackups.map(x => x.id);
        restoreJob.SelectedItems = [];

        let that = this;
        for (var instance of this.restoreJobSelectedInstances) {
            const instanceId = instance.id;
            let options = new RestoreOptions(this.selectedRegion || "", this.selectedProfileId);
            options.InstanceId = instanceId;

            let index = Helper.InArray(options, that.restoreInstanceOptions, "InstanceId");
            if (index >= 0) {
                options = that.restoreInstanceOptions[index];
            }
            const item = new RestoreJobItemViewModel(options, instanceId, "", "", "", "", "");

            restoreJob.SelectedItems.push(item);
        }

        for (var disk of this.restoreJobSelectedDisks) {
            const id = disk.id;
            let options = new RestoreOptions(this.selectedRegion || "", this.selectedProfileId);
            options.DiskId = id;

            let index = Helper.InArray(options, that.restoreDiskOptions, "DiskId");
            if (index >= 0) {
                options = that.restoreDiskOptions[index];
            }

            const item = new RestoreJobItemViewModel(options, "", id, "", "", "", "");

            restoreJob.SelectedItems.push(item);
        }

        this.backupService.addRestoreJob(restoreJob)
            .then((job) => {
                this.restoreJobCreated.emit(job);
                this.hide();
            });
    }

    onBackupSelectionChanged() {
        this.validateSelectedBackups();

        if (this.selectedBackupIds.length === 0)
            return;

        this.setInstances();
        this.setDisks();

    }

    private setInstances() {
        this.restoreJobInstancesLoading = true;
        this.selectedInstancesGrid.instance.beginCustomLoading('');
        this.restoreJobInstances = [];
        this.restoreInstanceOptions = [];
        let that = this;

        this.backupService.getBackupObjectInstancesList(this.selectedBackupIds, true).then((res: any) => {
            var instanceItems = (res != null ? res.items : null) || [];
            for (var instance of instanceItems) {
                if (Helper.InArray(instance, this.restoreJobInstances, "instanceId") < 0) {
                    this.restoreJobInstances.push(instance);

                    if (this.initialData != null) {
                        let initial = this.initialData.sourceObjectIds
                            .some(x => x.sourceObjectId === instance.id && x.type === JobObjectType.Instance);

                        if (initial) {
                            this.restoreJobSelectedInstances.push(instance);
                        }
                    }

                    var options = new RestoreOptions(that.selectedRegion || "", that.selectedProfileId);
                    options.InstanceId = instance.id || instance.Id || instance.InstanceId;
                    that.restoreInstanceOptions.push(options);
                }
            }

            if (this.restoreJobSelectedInstances.length > 0)
                DevExtremeHelper.scrollToRow(that.selectedInstancesGrid, this.restoreJobSelectedInstances[0]);

            that.restoreJobInstancesLoading = false;
            that.selectedInstancesGrid.instance.endCustomLoading();
        }, () => {
            this.restoreJobInstancesLoading = false;
            this.selectedInstancesGrid.instance.endCustomLoading();
        });
    }

    private setDisks() {
        this.restoreJobDisksLoading = true;
        this.restoreDiskOptions = [];
        this.selectedDisksGrid.instance.beginCustomLoading('');
        this.restoreJobDisks = [];
        let that = this;

        this.backupService.getBackupObjectDisksList(this.selectedBackupIds, this.showAttachedDisks, true)
            .then(disks => {
                for (var disk of disks) {
                    if (Helper.InArray(disk, that.restoreJobDisks, "id") < 0) {
                        that.restoreJobDisks.push(disk);

                        if (this.initialData != null) {
                            let initial = this.initialData.sourceObjectIds
                                .some(x => x.sourceObjectId === disk.id && x.type === JobObjectType.Disk);

                            if (initial) {
                                this.restoreJobSelectedDisks.push(disk);
                            }
                        }

                        var options = new RestoreOptions(that.selectedRegion || "", that.selectedProfileId);
                        options.DiskId = disk.id;
                        that.restoreDiskOptions.push(options);
                    }
                }

                if (this.restoreJobSelectedDisks.length > 0)
                    DevExtremeHelper.scrollToRow(that.selectedDisksGrid, this.restoreJobSelectedDisks[0]);

                that.restoreJobDisksLoading = false;
                that.selectedDisksGrid.instance.endCustomLoading();
            }).catch(() => {
                this.restoreJobDisksLoading = false;
                this.selectedDisksGrid.instance.endCustomLoading();
            });
    }

    // setRegions() {
    //     let that = this;
    //     this.backupService.getRegions().then(regions => that.regions = regions);
    // }

    // setProfiles() {
    //     let that = this;
    //     this.backupService.getProfiles().then(profiles => that.profiles = profiles);
    // }

    setRestoreLocationMode(type: number) {
        this.restoreLocationMode = type;

        if (this.restoreLocationMode === 0) {
            this.selectedRegion = '';
            this.selectedProfileId = 0;
        }
    }

    validateJobWizardData() {
        this.validateJobObjects();
    }

    validateSelectedInstances(): boolean {
        return this.restoreJobSelectedInstances.length !== 0;
    }

    validateSelectedDisks(): boolean {
        return this.restoreJobSelectedDisks.length !== 0;
    }

    validateJobObjects(): Boolean {
        this.validationMessageText = "At least one job object must be selected.";

        this.steps[1].IsValid = this.validateSelectedInstances() || this.validateSelectedDisks();

        if (this.steps[1].IsValid) {
            this.validationMessageText = "";
        }

        return this.steps[1].IsValid;
    }

    validateSelectedBackups(): Boolean {
        this.steps[0].IsValid = this.selectedBackups != null && this.selectedBackups.length > 0;

        return this.steps[0].IsValid;
    }

    setTenants() {
        return this.administrationService.getAllowedTenants()
            .then(tenants => {
                this.tenants = tenants;

                if (this.tenants.length > 0) {
                    this.selectedTenantId = this.tenants[0].Id;
                    this.selectedTenantIdChange.emit(this.selectedTenantId);
                }
            });
    }

    onTenantValueChanged(e: any) {
        // To avoid double setBackupsDataSource() call during initialization
        if (e.previousValue != null) {
            this.setBackupsDataSource();
        }
    }

    private setBackupsDataSource() {
        if (this.initialData == null || this.initialData.backupIds.length === 0) {
            if (this.selectedTenantId == null) {
                this.backups = [];
                return;
            }

            this.backups = this.backupService.getBackupsDataSourceByTenant(this.selectedTenantId, { restoreEligible: true, status: BackupStatus.Success });
        } else {
            // Initial backups are set
            this.backupService.getBackupsByIds(this.initialData.backupIds)
                .then(backups => {
                    this.backups = backups;
                    this.selectedBackups = backups;

                    // We need to select an appropriate tenant for initial backups
                    var selectedBackupsTenantIds = new Set(backups.map((x: any) => x.tenantId as number));
                    if (selectedBackupsTenantIds.size !== 1)
                        throw new Error('Initial backups must be from one tenant.');

                    this.selectedTenantId = selectedBackupsTenantIds.values().next().value;
                });
        }
    }

    showInstanceRestoreOptions() {
        this.instanceOptionsDialogVisible = true;
    }

    showDiskRestoreOptions() {
        this.diskOptionsDialogVisible = true;
    }


    onSelectedRegionChanged() {
        // TODO this can be avoided by passing destRegion directly to options dialog
        this.setInstanceRestoreOptions();
        this.setDiskRestoreOptions();
    }


    onSelectedProfileChanged() {
        // TODO this can be avoided by passing destRegion directly to options dialog
        this.setInstanceRestoreOptions();
        this.setDiskRestoreOptions();
    }

    setInstanceRestoreOptions() {
        const instanceOptions = Object.assign([], this.restoreInstanceOptions);
        this.restoreInstanceOptions = [];

        if (!this.restoreJobInstances) {
            return;
        }

        for (var i = 0; i < this.restoreJobInstances.length; i++) {
            var options = new RestoreOptions(this.selectedRegion || "", this.selectedProfileId);
            options.InstanceId = this.restoreJobInstances[i].instanceId;
            this.restoreInstanceOptions.push(options);
        }
    }

    setDiskRestoreOptions() {
        this.restoreDiskOptions = [];

        if (!this.restoreJobDisks) {
            return;
        }

        for (var i = 0; i < this.restoreJobDisks.length; i++) {
            var options = new RestoreOptions(this.selectedRegion || "", this.selectedProfileId);
            options.DiskId = this.restoreJobDisks[i].id;
            this.restoreDiskOptions.push(options);
        }
    }

    setObjectsTabs() {
        let tabs: any[] = [];

        tabs.push({ id: 0, text: "Instances" });
        tabs.push({ id: 1, text: "Disks" });

        this.restoreJobObjectTabs = tabs.concat(this.restoreJobObjectTabs);
        this.selectedRestoreJobObjectTab = this.restoreJobObjectTabs[0];
    }
}
