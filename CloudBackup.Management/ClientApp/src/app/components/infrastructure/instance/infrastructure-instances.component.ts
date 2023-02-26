//inner
import { Component, Injectable, Input, OnInit, ViewChild, OnDestroy } from '@angular/core';
import CustomStore from 'devextreme/data/custom_store';

import { DxDataGridComponent } from 'devextreme-angular';
import { Router, CanActivate, ActivatedRoute } from '@angular/router';
import { Profile } from 'src/app/classes/Profile';
import { Subject } from 'rxjs';
import { AppRoutes } from 'src/app/app.routes';
import { BackupJob } from 'src/app/classes/BackupJob';
import { InstanceFilter } from 'src/app/classes/InstanceFilter';
import { JobObject } from 'src/app/classes/JobObject';
import { BreadCrumb, NavigationMenuItem, NavigationMenu } from 'src/app/classes/NavigationMenu';
import { DevExtremeHelper } from 'src/app/helpers/DevExtremeHelper';
import { Helper } from 'src/app/helpers/Helper';
import { BackupService } from 'src/app/services/backup/BackupService';
import { MenuService } from 'src/app/services/common/MenuService';
import { NotificationService } from 'src/app/services/common/NotificationService';
import { PermissionService } from 'src/app/services/common/PermissionService';
import { Instance } from 'src/app/classes/Instance';
//import { BackupInstanceDisksComponent } from './disks/instance-disks.component';
import { debounceTime } from 'rxjs/operators';
import { CustomPermissions } from 'src/app/classes/UserPermissions';
import { CloudFolder } from 'src/app/classes/CloudFolder';
import { SortOrder } from 'src/app/classes/SortOrder';
import { instanceColumns } from 'src/app/classes/constants';
import { DataGridSettings } from '../../../classes';

@Component({
    templateUrl: './infrastructure-instances.template.html'
})

export class InfrastructureInstancesComponent implements OnInit, OnDestroy {
    static title = "Instances";
    static path = "Instances";
    public gridsettings = new DataGridSettings();
    //childPath = BackupInstanceDisksComponent.path;

    // filters
    profiles: Profile[] = [];
    selectedProfileId: number;

    //clouds and folders
    clouds: Array<any> = [];
    selectedCloud!: string;

    folders: Array<CloudFolder> = [];
    selectedFolderId!: string;

    searchSubject = new Subject<string>();
    filterVisible = false;
    filter: InstanceFilter = {};
    instanceStates: string[] = [];
    instance: Instance;

    //check this
    //private addBackupWidth = 190;

    // grid
    @ViewChild('backupInstancesGrid')
    grid: DxDataGridComponent;
    storageKey = 'backup-instances-grid';
    instancesDataSource: any = [];
    selectedRows: any[];
    selectedRowsTenantIds: number[] = []; // field is updated when selection changes

    useCache = false;
    intervalId: number;

    sortOrder: SortOrder = "asc";

    columns = instanceColumns;

    actionsVisible: boolean;
    isLoaded: boolean;

    // backup
    hasBackupPermission: boolean;
    get canBackup(): boolean {
        return this.hasBackupPermission && this.selectedRows.length > 0 && this.selectedRowsTenantIds.length === 1;
    }
    createJobPopupVisible = false;
    backupJobToCreate: BackupJob;

    canEdit: boolean;
    popupShow: boolean;

    get canStartInstances() {
        return this.canEdit && this.selectedRows.every(x => x.status === "STOPPED");
    }

    get canStopInstances() {
        return this.canEdit && this.selectedRows.every(x => x.status === "RUNNING");
    }

    get canRebootInstances() {
        return this.canEdit && this.selectedRows.every(x => x.status === "RUNNING");
    }

    get canTerminateInstances() {
        return this.canEdit && this.selectedRows.every(x => x.status === "RUNNING" || x.status === "STOPPED");
    }

    constructor(
        private backupService: BackupService,
        private notificator: NotificationService,
        private permissionService: PermissionService,
        private menu: MenuService,
        private router: Router,
        private route: ActivatedRoute) {

        this.selectedRows = [];
        this.actionsVisible = false;
        this.canEdit = true;

        this.searchSubject.pipe(
            debounceTime(500)
        ).subscribe(value => this.grid.instance.searchByText(value));
        //todo: check this
        /*
        var userAgent = window.navigator.userAgent.toLowerCase();
        if (userAgent.indexOf('mac') > -1 && userAgent.indexOf('chrome') < 0) {
            this.addBackupWidth = 210;
        }*/
    }

    ngOnInit() {
        this.selectedRows = [];
        // this.setRegions();
        this.menu.setFirstLevelMenu(AppRoutes.InfrastructureRoute);
        this.menu.setSecondLevelMenu(AppRoutes.BackupInstanceRoute);
        this.menu.addBreadcrumb(new BreadCrumb(InfrastructureInstancesComponent.title, InfrastructureInstancesComponent.path, 0));
        this.menu.setLocation(""); //set root location for empty navigation menu

        this.permissionService.getPermissions().then(permissions => {
            this.canEdit = (permissions.InfrastructureRights & CustomPermissions.Write) === CustomPermissions.Write;
            this.hasBackupPermission = (permissions.BackupRights & CustomPermissions.Write) === CustomPermissions.Write;
        });

        this.getClouds();

        this.backupService.getInstanceStates().then(result => this.instanceStates = result);

        this.selectedProfileId = this.backupService.getSelectedProfileFilter();

        this.intervalId = window.setInterval(() => {
            this.useCache = true;
            this.grid.instance.option('loadPanel.enabled', false);
            this.update()
                .catch(() => console.error("Failed to auto-update backup instances"))
                .then(() => {
                    this.useCache = false;
                    this.grid.instance.option('loadPanel.enabled', true);
                });
        },
            10000);

        this.route.queryParams.subscribe(params => {
            const region = params["region"] as string;

            const profileId = params["profileId"] as string;
            if (profileId) {
                this.selectedProfileId = profileId.toLowerCase() === "all" ? 0 : parseInt(profileId) || 0;
                this.backupService.setSelectedProfileFilter(this.selectedProfileId);
            }

            this.filter = {
                state: params["state"] as string,
                platform: params["platform"] as string,
                hasBackups: Helper.BooleanFromString(params["hasBackups"])
            };


            if (Object.keys(params).filter(x => x !== "region" && x !== "profileId").length > 0)
                this.filterVisible = true;

            this.updateDataSource();
        });
    }

    ngOnDestroy(): void {
        clearInterval(this.intervalId);
    }

    update() {
        return Promise.resolve(this.grid.instance.refresh());
    }

    private updateDataSource() {
        const filter = Object.assign({}, this.filter);
        const store = this.backupService.getAllInstancesDataSource(this.selectedFolderId, false, filter);
        const cachedStore = this.backupService.getAllInstancesDataSource(this.selectedFolderId, true, filter);

        // we override datasource so that underlying datasource will depend on this.useCache
        this.instancesDataSource = new CustomStore({
            load: (loadOptions: Object) => {
                let currentStore = this.useCache ? cachedStore : store;
                return currentStore.load(loadOptions);
            }
        });
    }

    onSearchValueChanged(e: any) {
        this.searchSubject.next(e.value);
    }

  showActions() {
    if (this.canBackup || this.canStartInstances || this.canStopInstances || this.canRebootInstances || this.canTerminateInstances)
        this.actionsVisible = !this.actionsVisible;
    }

    getClouds = () => {
        this.backupService.getClouds().then(clouds => {
            //this.clouds = clouds.map((x: any) => x.name);
            this.clouds = clouds;
            console.log(this.clouds);
            if (this.clouds && this.clouds.length === 1)
                this.selectedCloud = this.clouds[0].id;
        });
    }

    getFolders = () => {
        if (this.selectedCloud != null) {
            this.backupService.getFolders(this.selectedCloud).then((folders: Array<CloudFolder>) => {
                this.folders = folders;
                if (this.folders && this.folders.length === 1)
                    this.selectedFolderId = this.folders[0].id || "";
            });
        }
    }


    // setRegions() {
    //     this.backupService.getRegions().subscribe(regions => this.regions = regions);
    // }

    // editInstance = () =>  {
    //     if (this.selectedRows.length > 0) {
    //         console.log("instances: opening edit dialog...");

    //         var instance = Object.assign({}, this.selectedRows[0]);
    //         this.instance = Instance.Copy(instance);
    //         this.instance.SelectedProfile = this.backupService.getSelectedProfile();
    //         this.popupShow = true;
    //     }
    // }

    startInstances = () => {
        this.actionsVisible = false;

        const action = (instanceIds: string[]) => this.backupService.startInstances(instanceIds);
        this.notificator.confirmYesNo(
            confirmed => this.processInstances(confirmed, action),
            "Are you sure you want to start these instances?",
            "Start instances");
    }

    stopInstances = () =>  {
        this.actionsVisible = false;

        const action = (instanceIds: string[]) => this.backupService.stopInstances(instanceIds);
        this.notificator.confirmYesNo(
            confirmed => this.processInstances(confirmed, action),
            "Are you sure you want to stop these instances?",
            "Stop instances");
    }

    rebootInstances = () =>  {
        this.actionsVisible = false;

        const action = (instanceIds: string[]) => this.backupService.rebootInstances(instanceIds);
        this.notificator.confirmYesNo(
            confirmed => this.processInstances(confirmed, action),
            "Are you sure you want to reboot these instances?",
            "Reboot instances");
    }

    terminateInstances = () =>  {
        this.actionsVisible = false;

        const action = (instanceIds: string[]) => this.backupService.terminateInstances(instanceIds);
        this.notificator.confirmYesNo(
            confirmed => this.processInstances(confirmed, action),
            "Are you sure you want to terminate these instances?",
            "Terminate instances");
    }

    private processInstances(confirmed: boolean, action: (instanceIds: string[]) => Promise<any>) {
        if (!confirmed)
            return;

        const instanceIds = this.selectedRows.map(x => x.id);
        action(instanceIds)
            .then(() => this.update())
            .catch(err => this.notificator.showErrorNotification(Helper.GetErrorMessage(err)));
    }

    addToBackupJob() {
        this.actionsVisible = false;

        if (this.selectedRowsTenantIds.length !== 1) {
            this.notificator.showErrorNotification("Can't create backup jobs with objects from multiple tenants.");
            return;
        }

        this.backupJobToCreate = new BackupJob();
        this.backupJobToCreate.TenantId = this.selectedRowsTenantIds[0];
        this.backupJobToCreate.JobObjects = this.selectedRows.map(x => JobObject.FromInstance(x));

        this.createJobPopupVisible = true;
    }

    onBackupJobCreated(job: BackupJob) {
        this.router.navigate(['./' + AppRoutes.BackupJobsRoute.path]);
    }

    onFilterValueChanged() {
        const queryParams = {
            profileId: this.selectedProfileId || "all",
            state: this.filter.state,
            platform: this.filter.platform,
            hasBackups: Helper.Coalesce(this.filter.hasBackups, null)
        }
        this.router.navigate(["./"], { queryParams: queryParams, relativeTo: this.route });
    }

    onSelectionChanged() {
        const selectedProfileIds = this.selectedRows.map(x => x.selectedProfile as number);
        const selectedTenantIds = new Set(this.profiles.filter(x => selectedProfileIds.indexOf(x.Id) !== -1).map((x:any) => x.Tenant.Id));
        this.selectedRowsTenantIds = Array.from(selectedTenantIds);
    }

    onCloudChanged() {
        if (this.selectedCloud != null) {
            this.backupService.setSelectedCloud(this.selectedCloud);
            this.getFolders();
        }
    }
    
    onValueChanged() {
        if (this.selectedFolderId != null) {
            this.backupService.setSelectedFolder(this.selectedFolderId);
        }

        this.updateDataSource();
    }

    // setMenu() {
    //     const items = this.grid.instance.getDataSource().items().map(
    //         (x: any) => new NavigationMenuItem(x.instanceId, x.name || x.instanceId, x, this.childPath, ''));
    //     const navMenu = new NavigationMenu(InfrastructureInstancesComponent.path, items, this.route);
    //     this.menu.setNavigation(navMenu); //set new navigation menu binded to "Instances" address
    // }

    customLoad() {
        return DevExtremeHelper.loadGridState(this.storageKey);
    }

    customSave(state: any) {
        DevExtremeHelper.saveGridState(this.storageKey, state);
    }

    customizeColumns(columns: any) {
        for (var i = 0; i < columns.length; i++) {
            columns[i].width = "auto";
        }
    }

    onContextMenuPreparing(e: any) {
        if (e.row != null && e.row.rowType === "data") {
            if (!e.row.isSelected)
                this.grid.instance.selectRows([e.row.key], false);

            e.items = [
                {
                    icon: "create-backup",
                    text: "Add to backup job",
                    disabled: !this.canBackup,
                    template: "contextMenuItemTemplate",
                    onItemClick: () => this.addToBackupJob()
                },
                {
                    icon: "resume",
                    text: "Start",
                    disabled: !this.canStartInstances,
                    template: "contextMenuItemTemplate",
                    onItemClick: () => this.startInstances()
                },
                {
                    icon: "stop",
                    text: "Stop",
                    disabled: !this.canStopInstances,
                    template: "contextMenuItemTemplate",
                    onItemClick: () => this.stopInstances()
                },
                {
                    icon: "retry",
                    text: "Reboot",
                    disabled: !this.canRebootInstances,
                    template: "contextMenuItemTemplate",
                    onItemClick: () => this.rebootInstances()
                },
                {
                    icon: "delete",
                    text: "Terminate",
                    disabled: !this.canTerminateInstances,
                    template: "contextMenuItemTemplate",
                    onItemClick: () => this.terminateInstances()
                }
            ];
        }
    }
}
