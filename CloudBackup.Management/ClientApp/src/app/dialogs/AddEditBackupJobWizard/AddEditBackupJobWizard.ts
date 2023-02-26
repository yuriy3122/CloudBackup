//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, DoCheck, ViewChild, ChangeDetectorRef } from '@angular/core';
import { DxListComponent, DxDataGridComponent } from 'devextreme-angular';

//services
import { NotificationService } from '../../services/common/NotificationService';
import { BackupService } from '../../services/backup/BackupService';
import { AdministrationService } from "../../services/administration/AdministrationService";
import { PermissionService } from "../../services/common/PermissionService";
//classes
import { WizardStep } from '../AddEditWizard/AddEditWizard';
import { BackupJob } from '../../classes/BackupJob';
import { Schedule } from '../../classes/Schedule';
import { JobObject } from '../../classes/JobObject';
import { Guid } from '../../classes/Guid';
import { JobObjectType, OccurType, StartupType } from '../../classes/Enums';
//helper
import { Helper } from '../../helpers/Helper';
import * as moment from 'moment';

import { DelayedScheduleParam } from "../../classes/DelayedScheduleParam";
import { JobOptions, BackupAfterErrorStatus, InstanceBackupMode, TagJoinType } from "../../classes/JobOptions";
import { RetentionPolicy } from "../../classes/RetentionPolicy";
import { ReplicationOptions } from "../../classes/ReplicationOptions";
import { Profile } from "../../classes/Profile";
import { Tenant } from "../../classes/Tenant";
import { JobScheduleEditorComponent } from "../../components/common/job-schedule-editor/job-schedule-editor.component";
import { User } from "../../classes/User";
import { Disk } from "../../classes/Disk";
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { diskColumns, instanceColumns } from 'src/app/classes/constants';
import { DataGridSettings } from '../../classes';

enum ScheduleDayGroupOption {
    EveryDay = 0,
    WeekDays = 1,
    TheseDays = 2
}
enum ScheduleDayOfMonthGroupOption {
    First = 1,
    Second = 2,
    Third = 3,
    Fourth = 4,
    Last = -1,
    ThisDay = 0
}

@Component({
    selector: 'add-edit-backup-job-wizard',
    templateUrl: './AddEditBackupJobWizard.template.html'
})
@Injectable()
export class AddEditBackupJobWizard implements OnInit, DoCheck {
    public gridsettings = new DataGridSettings();
    //basic data
    public guid!: string;
    //locker
    public clicked: boolean = false;
    //backupJob
    @Input()
    public backupJob: BackupJob = new BackupJob();
    //done function
    @Output()
    complete = new EventEmitter<BackupJob>();
    @Output()
    backupJobChange = new EventEmitter();
    //wizard data
    //steps for wizard
    public steps!: Array<WizardStep>;
    //wizard options
    //step number
    public stepIndex!: number;
    // to reference from template
    StartupType = StartupType;
    //show popup or not
    @Input()
    public popupVisible!: boolean;
    @Output() popupVisibleChange = new EventEmitter();

    //instances list
    public instances: Array<any> = [];
    //selected instances list
    public selectedInstances: Array<any> = [];
    //select instances popup
    public selectInstancesPopupVisible: boolean = false;

    //disks list
    public disks: Array<Disk> = [];
    //selected disks list
    public selectedDisks: Array<Disk> = [];
    //select disks popup
    public selectDisksPopupVisible: boolean = false;

    // schedules
    public startupType = StartupType.Immediately;

    public delayedRunTime: any = new Date();

    public schedules: Array<Schedule> = []; // all schedules
    public scheduleId: number = 0; // id of schedule being edited

    @ViewChild("scheduleEditor")
    scheduleEditor!: JobScheduleEditorComponent;
    public recurrentSchedules: Array<Schedule> = []; // only recurrent schedules for selection
    selectedRecurrentSchedule!: Schedule;
    recurrentScheduleToEdit!: Schedule;
    showRecurrentScheduleValidation = false;

    // retention
    public canSelectRetentionMode: boolean = false;

    // Cost
    backupJobForCost: BackupJob = new BackupJob();
    public jobCostChangeSubject = new Subject();

    @ViewChild("instancesGrid")
    instancesGrid!: DxDataGridComponent;
    @ViewChild("disksGrid")
    disksGrid!: DxDataGridComponent;

    //error messages
    public nameMessage!: string;
    public instancesMessage!: string;

    //locker
    public lock!: boolean;

    public instanceColumns = instanceColumns;
    public diskColumns = diskColumns;

    public jobObjectTabs: { id: number, text: string }[] = [];
    public selectedJobObjectTab!: { id: number; text: string; };
    public title!: string;

    get windowsInstancesSelected(): boolean {
        return this.instances.some(x => x.platform === 'Windows');
    }

    public restorePointsToKeep: number = 10;
    public retentionTimeIntervalValue: number = 14;
    public retentionTimeIntervalType: number = 0;
    public retentionAction: number = 0;

    public isDelayedRunTimeValid: boolean = true;
    public isRecurrentScheduleValid: boolean = true;
    public replicationValid: boolean = true;

    public retentionTimeIntervalTypes = [
        { text: "days", id: 0 },
        { text: "hours", id: 1 },
        { text: "months", id: 2 }
    ];

    public retentionActions = [
        { text: "Remove old backups", id: 0 },
        { text: "Move old backups to archive", id: 1 }
    ];

    public clouds: Array<string> = [];
    public profiles: Profile[] = [];

    //Selected Objects for Summary
    public selectedObjects: { key: string, value: string }[] = []

    public currentUser!: User;

    public tenants: Array<Tenant> = [];

    constructor(public notificator: NotificationService,
        public backupService: BackupService,
        public administrationService: AdministrationService,
        public permissionService: PermissionService,
        public changeDetector: ChangeDetectorRef) {

        this.jobCostChangeSubject.pipe(
            debounceTime(500)
        ).subscribe(() => this.updateCost());

        console.log('add-edit-backup-job-wizard');
    }

    ngDoCheck() {
        this.popupVisibleChange.emit(this.popupVisible);
        this.backupJobChange.emit(this.backupJob);
    }

    ngOnInit() {
        this.stepIndex = 0;
        this.setClouds();

        this.setObjectsTabs();

        this.selectedJobObjectTab = this.jobObjectTabs[0];

        this.steps = [
            new WizardStep(
                () => { },
                () => this.validateCommonInfo(),
                "Name",
                "Specify name and description for job"),
            new WizardStep(
                () => { },
                () => this.validateJobObjects(),
                "Objects",
                "Select objects to process"),
            new WizardStep(
                () => { },
                () => this.validateScheduleParams(),
                "Schedule",
                "Specify the job scheduling options"),
            new WizardStep(
                () => { },
                () => {
                    this.updateJobObjectsSummary();
                    return true;
                },
                "Other Settings",
                "Define job retention policy and other additional settings"),
            new WizardStep(
                () => { },
                () => true,
                "Summary",
                "Review job settings and press Finish to run job")
        ];
    }

    onShow() {
        this.clicked = false;

        for (var step of this.steps) {
            step.IsValid = true;
        }

        this.setTenants();
        this.instances = [];
        this.selectedInstances = [];
        this.disks = this.selectedDisks = [];
        this.restorePointsToKeep = 10;
        this.retentionTimeIntervalValue = 14;
        this.retentionTimeIntervalType = 0;
        this.retentionAction = 0;
        this.setProfiles();
        this.setSchedules();
        this.getJobOptions();
        this.stepIndex = 0;
        this.lock = false;
        this.guid = Guid.NewGuid().ToString();
        this.selectedJobObjectTab = this.jobObjectTabs[0];
        this.replicationValid = true;

        for (let i = 0; i < this.steps.length; i++)
            this.steps[i].IsValid = true;

        this.permissionService.getCurrentUser().then(result => this.currentUser = result);
        // Filling JobObjects info. 
        // We do this even for new BackupJobs(Id = 0) because it can have predefined objects (for example, when creating job from selected instances).
        const fillJobObjectsInfo =
            (type: JobObjectType, fillItemsFunc: (folderId: string, ids: string[]) => Promise<void>) => {
                let items: Array<any> = [];
                if (this.backupJob)
                    items = this.backupJob.JobObjects.filter(q => { return q.Type === type });

                // unique pairs of folder-profile
                var profileFolders = items
                    .map(x => ({ ProfileId: x.ProfileId, FolderId: x.FolderId }))
                    .filter((item, i) =>
                        items.findIndex(x => x.ProfileId === item.ProfileId && x.FolderId === item.FolderId) === i);

                let promises: Promise<void>[] = [];
                for (let profileFolder of profileFolders) {
                    var itemsIds: string[] = [];

                    for (var i = 0; i < items.length; i++) {
                        if (items[i].ProfileId == profileFolder.ProfileId && items[i].FolderId == profileFolder.FolderId) {
                            itemsIds.push(items[i].ObjectId);
                        }
                    }

                    promises.push(fillItemsFunc(profileFolder.FolderId, itemsIds));
                }
                return Promise.all(promises);
            };

        let fillJobObjectPromises: Promise<any>[] = [];
        fillJobObjectPromises.push(
            fillJobObjectsInfo(JobObjectType.Instance,
                (folderId, ids) => {
                    return this.backupService.getBackupInstancesList(folderId, ids)
                        .then((res: any) => {
                            const items = (res != null && res.items != null ? res.items : []);
                            this.instances = this.instances.concat(items);

                            this.onJobObjectsChanged();
                        });
                }));

        fillJobObjectPromises.push(
            fillJobObjectsInfo(JobObjectType.Disk,
                (folderId, ids) => {
                    return this.backupService.getDisks(folderId, ids)
                        .then(items => {
                            this.disks = this.disks.concat(items);

                            this.onJobObjectsChanged();
                        });
                }));

        Promise.all(fillJobObjectPromises).then(() => {
            if (this.backupJob && this.backupJob.Id === 0) {
            }
        });

        this.title = this.backupJob == null || this.backupJob.Id === 0 ? "New Backup Job" : "Edit Backup Job";

        this.changeDetector.detectChanges();
    }
    backclass = { class: 'button secondary back' }
    onHiding = () => {
        this.popupVisible = false;
        this.popupVisibleChange.emit(false);
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
            this.hide();
    }

    onComplete = () => {
        this.validateJobWizardData();

        for (var i = 0; i < this.steps.length; i++) {
            if (!this.steps[i].IsValid) {
                this.stepIndex = i;
                return;
            }
        }

        if (!this.lock) {
            const job = this.getBackupJob();

            if (job.Id !== 0) {
                this.backupService.updateBackupJob(job)
                    .then(() => {
                        this.complete.emit(job);
                        this.hide();
                        this.lock = false;
                    }, (error) => {
                        console.log(error);
                        this.lock = false;
                    });
            }
            else {
                this.lock = true;
                this.backupService.addBackupJob(job)
                    .then(job => {
                        this.complete.emit(job);
                        this.hide();
                        this.lock = false;
                    }, (error) => {
                        console.log(error);

                        if (error.error.message == "backedUpInstancesLimit") {
                            this.stepIndex = 1;
                            this.lock = false;
                        }
                    });
            }
        }
    }

    setProfiles() {
        if (this.backupJob && this.backupJob.TenantId > 0) {
            this.backupService.getProfilesForTenant(this.backupJob.TenantId).then(profiles => this.profiles = profiles);
        }
    }

    setSchedules() {
        if (this.backupJob == null || this.backupJob.TenantId === 0)
            return;

        this.backupService.getSchedules(this.backupJob.TenantId)
            .then(schedules => {
                // initialize data
                this.schedules = schedules;

                this.recurrentSchedules = new Array<Schedule>();
                this.recurrentSchedules.push(Schedule.Copy({ Id: 0, Name: 'Create new', StartupType: StartupType.Recurring, OccurType: OccurType.Periodically }));
                this.recurrentSchedules.push(...this.schedules
                    .filter(x => x.StartupType === StartupType.Recurring)
                    .map(x => Object.assign({}, x))); // map is needed to clone schedules)

                // reset values to default
                this.startupType = StartupType.Immediately;
                this.delayedRunTime = new Date();
                this.isDelayedRunTimeValid = true;
                this.recurrentScheduleToEdit = new Schedule();
                this.showRecurrentScheduleValidation = false;

                // set current values
                this.scheduleId = this.backupJob && this.backupJob.Schedule != null ? this.backupJob.Schedule.Id : 0;
                this.setScheduleParams(this.scheduleId);
            });
    }

    hide = () => {
        this.popupVisible = false;
        this.popupVisibleChange.emit(false);
    }

    setStepIndex(step: number) {
        this.stepIndex = step || 0;
        this.updateJobObjectsSummary();
    }

    showSelectInstancesPopup() {
        this.selectInstancesPopupVisible = true;
    }

    showSelectDisksPopup() {
        this.selectDisksPopupVisible = true;
    }

    onSelectedInstance = (selectedInstances: Array<any>) => {
        selectedInstances = selectedInstances || [];

        for (var i = 0; i < selectedInstances.length; i++) {
            var index = Helper.InArray(selectedInstances[i], this.instances, "id");
            if (index < 0) {
                this.instances.push(selectedInstances[i]);
            }
        }
    }

    onSelectedDisk = (selectedDisks: Array<any>) => {
        selectedDisks = selectedDisks || [];

        for (var i = 0; i < selectedDisks.length; i++) {
            var index = Helper.InArray(selectedDisks[i], this.disks, "id");
            if (index < 0) {
                this.disks.push(selectedDisks[i]);
            }
        }
    }

    deleteInstance = () => {
        if (this.backupJob) {
            for (var i = 0; i < this.selectedInstances.length; i++) {
                var index = Helper.InArray(this.selectedInstances[i], this.instances, "id");
                if (index > -1) {
                    this.instances.splice(index, 1);
                }
            }

            this.onJobObjectsChanged();
        }
    }

    deleteDisk() {
        for (var i = 0; i < this.disks.length; i++) {
            var index = Helper.InArray(this.selectedDisks[i], this.disks, "id");
            if (index > -1) {
                this.disks.splice(index, 1);
                this.onJobObjectsChanged();
            }
        }
    }

    onJobObjectsChanged() {
        this.validateJobObjects();
        this.requestCostUpdate();
    }

    setStartupType(type: StartupType) {
        this.startupType = type;
        this.onScheduleParamsChanged();
    }

    onSelectedScheduleChanged(e: any) {
        if (e != null) {
            this.setScheduleParams(e.value);
        }
        this.onScheduleParamsChanged();
    }

    onScheduleParamsChanged() {
        this.requestCostUpdate();
    }

    setScheduleParams(id: number) {
        let schedule = this.schedules.find(x => x.Id === id);

        if (schedule == null) {
            this.recurrentScheduleToEdit = new Schedule();
            return;
        }

        this.startupType = schedule.StartupType;
        var param = schedule.Params ? JSON.parse(schedule.Params) : null;

        switch (this.startupType) {
            case StartupType.Delayed:
                let delayedParam = DelayedScheduleParam.Copy(param);
                this.delayedRunTime = Helper.GetDateFromISODateStr(delayedParam.RunAtDateTime);
                break;
            case StartupType.Recurring:
                this.recurrentScheduleToEdit = schedule;
                break;
        }
    }

    public getBackupJob() {
        const job = BackupJob.Copy(this.backupJob);

        const instances = this.instances.map(x => JobObject.FromInstance(x));
        const disks = this.disks.map(x => JobObject.FromDisk(x));

        job.JobObjects = disks.concat(instances);
        job.Schedule = this.getSelectedSchedule();
        job.JobOptions = this.getSelectedJobOptions();

        return job;
    }

    getJobOptions() {
        if (this.backupJob && this.backupJob.JobOptions != null && this.backupJob.JobOptions != "") {
            var options = JSON.parse(this.backupJob.JobOptions);
            var jobOptions = JobOptions.Copy(options);

            this.restorePointsToKeep = jobOptions.RetentionPolicy.RestorePointsToKeep;
            this.retentionTimeIntervalValue = jobOptions.RetentionPolicy.TimeIntervalValue;
            this.retentionTimeIntervalType = jobOptions.RetentionPolicy.RetentionTimeIntervalType;
            this.retentionAction = jobOptions.RetentionPolicy.RetentionAction;
        }
    }

    getSelectedJobOptions(): string {
        var policy = new RetentionPolicy(this.restorePointsToKeep, this.retentionTimeIntervalValue, this.retentionTimeIntervalType, this.retentionAction);
        var replication = new ReplicationOptions(false, 1, false, 0);
        var options = new JobOptions(policy, replication);
        return JSON.stringify(options);
    }

    getSelectedSchedule(): Schedule {
        var schedule = new Schedule(this.scheduleId);
        schedule.StartupType = this.startupType;
        schedule.TenantId = this.backupJob ? this.backupJob.TenantId : 0;

        if (this.startupType === StartupType.Delayed) {
            var runAtDateTime = Helper.GetDateTimeISOFormated(this.delayedRunTime);
            var delayedScheduleParam = new DelayedScheduleParam(runAtDateTime);
            schedule.Params = JSON.stringify(delayedScheduleParam);
        }
        else if (this.startupType === StartupType.Recurring) {
            schedule.Name = this.recurrentScheduleToEdit.Name;
            schedule.OccurType = this.recurrentScheduleToEdit.OccurType;
            schedule.Params = this.recurrentScheduleToEdit.Params;
        }

        return schedule;
    }

    setClouds() {
        this.backupService.getClouds().then(clouds => {
            this.clouds = clouds.map((x: any) => x.name);
        });
    }

    updateJobObjectsSummary() {
        this.selectedObjects = [];

        for (var instance of this.instances) {
            this.selectedObjects.push({ key: instance.id, value: instance.name || instance.id });
        }
        for (var disk of this.disks) {
            this.selectedObjects.push({ key: disk.id, value: disk.name || disk.id });
        }
    }

    setTenants() {
        let that = this;
        this.administrationService.getAllowedTenants().then(tenants => {
            that.tenants = tenants;

            if (that.tenants.length > 0 && that.backupJob && that.backupJob.TenantId === 0) {
                that.backupJob.TenantId = that.tenants[0].Id;
            }
        });
    }

    onTenantValueChanged(e: any) {
        this.instances = [];
        this.disks = [];

        this.validateJobObjects();
        this.scheduleId = 0;
        this.setProfiles();
        this.setSchedules();
    }

    requestCostUpdate() {
        this.jobCostChangeSubject.next();
    }

    public updateCost = () => {
        const isValid = this.validateJobObjects(false)
            && this.validateScheduleParams(false);

        if (!isValid) {
            this.backupJobForCost = new BackupJob();
            return;
        }

        this.backupJobForCost = this.getBackupJob();
    }

    customizeColumns(columns: any) {
        for (var i = 0; i < columns.length; i++) {
            columns[i].width = "auto";
        }
    }

    validateJobWizardData(updatesteps = true) {
        this.validateCommonInfo(updatesteps);
        this.validateJobObjects(updatesteps);
        this.validateScheduleParams(updatesteps);
    }

    public validateCommonInfo(updateStep = true): Boolean {
        if (!this.backupJob)
            return false;

        const isValid = this.backupJob.Name != null && this.backupJob.Name != '' && this.backupJob.TenantId > 0;

        if (updateStep)
            this.steps[0].IsValid = isValid;

        return isValid;
    }

    public validateJobObjects(updateStep = true): Boolean {
        const isValid = this.instances.length > 0 ||
            this.disks.length > 0;

        if (updateStep)
            this.steps[1].IsValid = isValid;

        if (isValid)
            this.updateJobObjectsSummary();
        return isValid;
    }

    public validateScheduleParams(updateStep = true): Boolean {
        this.isDelayedRunTimeValid = true;

        if (this.startupType === StartupType.Delayed && this.currentUser && this.currentUser.UtcOffset && this.currentUser.UtcOffset.Offset) {
            const nowInUserTime =
                moment().utc().add(this.currentUser.UtcOffset.Offset.TotalMinutes, "minutes").local(true).toDate();
            if (this.delayedRunTime == null ||
                this.delayedRunTime < nowInUserTime)
                this.isDelayedRunTimeValid = false;
        }

        const isValid = this.isDelayedRunTimeValid && (this.startupType !== StartupType.Recurring || this.isRecurrentScheduleValid);

        this.changeDetector.detectChanges();

        if (updateStep) {
            this.steps[2].IsValid = isValid;
            this.showRecurrentScheduleValidation = true;
        }

        return isValid;
    }

    setObjectsTabs() {
        let tabs: any[] = [];
        tabs.push({ id: 0, text: "Instances" });
        tabs.push({ id: 1, text: "Disks" });

        this.jobObjectTabs = tabs.concat(this.jobObjectTabs);
        this.selectedJobObjectTab = this.jobObjectTabs[0];
    }

    onInstanceRowClick(e: any) {
        if (e.rowType === 'data') {
            if (e.isSelected) {
                this.instancesGrid.instance.deselectRows([e.key]);
            } else {
                this.instancesGrid.instance.selectRows([e.key], true);
            }
        }
    }

    onDiskRowClick(e: any) {
        if (e.rowType === 'data') {
            if (e.isSelected) {
                this.disksGrid.instance.deselectRows([e.key]);
            } else {
                this.disksGrid.instance.selectRows([e.key], true);
            }
        }
    }
}

