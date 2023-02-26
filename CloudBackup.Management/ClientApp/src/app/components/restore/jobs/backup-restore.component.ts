//inner
import { Component, Injectable, Input, OnInit, ViewChild } from '@angular/core';
import { DxDataGridComponent } from 'devextreme-angular';
//services
import { BackupService } from '../../../services/backup/BackupService';
import { NotificationService } from '../../../services/common/NotificationService';
import { MenuService } from '../../../services/common/MenuService';
import { PermissionService } from '../../../services/common/PermissionService';
//classes
import { AppRoutes } from '../../../app.routes';
import { BreadCrumb } from '../../../classes/NavigationMenu';
import { CustomPermissions, UserPermissions } from '../../../classes/UserPermissions';
//helper
import { DevExtremeHelper } from "../../../helpers/DevExtremeHelper";
import { DataGridSettings } from '../../../classes';

@Component({
    templateUrl: './backup-restore.template.html'
})

@Injectable()
export class BackupRestoreComponent implements OnInit {
    static title = "Jobs";
    static path = "RestoreJobs";
    public gridsettings = new DataGridSettings();
    public createJobPopupVisible: boolean;

    @ViewChild('backupRestoreGrid')
    private grid: DxDataGridComponent;
    public storageKey = 'backup-restore-grid';
    public restoreJobs: any = [];
    public selectedRowKeys: number[];
    public showPager: boolean;

    public hideFilters: boolean;
    public actionsVisible: boolean;
    public isLoaded: boolean;
    public showLog: boolean;
    public logEntries: string[] = [];

    canEdit: boolean;
    get canShowLog(): boolean {
        if (this.selectedRowKeys.length !== 1 || this.grid.instance == null)
            return false;
        var selectedRow = this.grid.instance.getSelectedRowsData()[0];
        return selectedRow.log != null && selectedRow.log.length !== 0;
    }

    public menuItems = [
        { text: 'VIEW LOG', icon: 'log' }
    ];

    public columns = [
        { caption: "Name", dataField: "name" },
        { caption: "Objects", dataField: "objectCount" },
        { caption: "Start Time", dataField: "startedAt" },
        { caption: "End Time", dataField: "finishedAt" },
        { caption: "Status", dataField: "status" },
        { caption: "Result", dataField: "result", cellTemplate: "restoreJobResultTemplate", width: "auto" }
    ];

    //callback
    private delayedGetRestoreJobs: Function;
    private socket: WebSocket;

    public static checkPermissions(permissions: UserPermissions): boolean {
        return (permissions.RestoreRights & CustomPermissions.Read) === CustomPermissions.Read;
    }

    constructor(
        private backupService: BackupService,
        private notificator: NotificationService,
        private menu: MenuService,
        private permissionService: PermissionService) {

        this.restoreJobs = backupService.getRestoreJobsDataSource() || [];
        this.selectedRowKeys = [];
        this.showLog = false;
        this.hideFilters = true;
        this.showPager = true;
        this.actionsVisible = false;

        let that = this;

        this.delayedGetRestoreJobs = () => that.update();
    }

    ngOnInit() {
        this.menu.setFirstLevelMenu(AppRoutes.RestoreRoute);
        this.menu.setSecondLevelMenu(AppRoutes.RestoreJobRoute);
        this.menu.addBreadcrumb(new BreadCrumb(BackupRestoreComponent.title, BackupRestoreComponent.path, 0));
        this.initNotifications();
        this.selectedRowKeys = [];
        this.canEdit = true;

        let that = this;
        this.permissionService.getPermissions().then(permissions => {
            if (!BackupRestoreComponent.checkPermissions(permissions))
                alert("Access to restore jobs with insufficient rights.");

            that.canEdit = (permissions.RestoreRights & CustomPermissions.ReadWrite) === CustomPermissions.ReadWrite;
        });
    }

    ngOnDestroy() {
        this.socket.onclose = () => undefined;
        this.socket.close();
    }

    private initNotifications() {
        this.socket = this.notificator.getSocket("notifications");
        this.socket.onmessage = (ev: MessageEvent) => {
            if (ev.data === "RestoreJobList") {
                this.update();
            }
        };
        this.socket.onclose = () => setTimeout(() => this.initNotifications(), 5000);
    }

    showFiltersClick() {
        this.hideFilters = !this.hideFilters;
    }

    onSearchValueChanged(e: any) {
        this.grid.instance.searchByText(e.value);
    }

    showActions() {
        this.actionsVisible = !this.actionsVisible;
    }

    update() {
        this.grid.instance.refresh();
    }

    getArrayLength(value: any): number {
        let that = this;
        if (value == null || value.value == null)
            return 0;
        return value.value.length;
    }

    customLoad() {
        return DevExtremeHelper.loadGridState(this.storageKey);
    }

    customSave(state: any) {
        DevExtremeHelper.saveGridState(this.storageKey, state);
    }

    itemClick(e: any) {
        if (!e.itemData.items) {
            console.log(e.itemData.text);
        }
    }

    viewLog() {
        this.logEntries = [];

        if (this.selectedRowKeys.length === 1) {
            var logs = this.grid.instance.getSelectedRowsData()[0].log || [];
            this.logEntries = logs;

            this.showLog = true;
        }
    }

    createJob() {
        this.createJobPopupVisible = true;
    }

    onRestoreJobCreated() {
        this.update();
        // Scrolling to row is disabled because of autorefresh:
        // when scrolled to newly created row, it can disappear after it's state or run date changed
        //DevExtremeHelper.scrollToRow(this.grid, restoreJobId);
    }

    onRowClick(e: any) {
        if (e.rowType === 'data') {
            if (e.isSelected) {
                this.grid.instance.deselectRows([e.key]);
            } else {
                this.grid.instance.selectRows([e.key], true);
            }
        }
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

            e.items = [{
                icon: "log",
                text: "View Log",
                template: (template: any) => `<div class='icon-bar management'><div class='icon ${template.icon}'><span>${template.text}</span></div></div>`,
                onItemClick: () => this.viewLog()
            }];
        }
    }
}
