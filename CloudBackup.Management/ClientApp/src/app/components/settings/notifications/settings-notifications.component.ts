//inner
import { Component, Injectable, OnInit, ViewChild } from '@angular/core';
//services
import { AdministrationService } from '../../../services/administration/AdministrationService';
import { NotificationService } from '../../../services/common/NotificationService';
import { MenuService } from '../../../services/common/MenuService';
//classes
import { AppRoutes } from '../../../app.routes';
import { BreadCrumb } from '../../../classes/NavigationMenu';
//helper
import { Helper } from '../../../helpers/Helper';
import { DevExtremeHelper } from '../../../helpers/DevExtremeHelper';
import { Profile, ProfileState } from "../../../classes/Profile";
import { NotificationConfig } from "../../../classes/NotificationConfig";
import { CustomPermissions, UserPermissions } from "../../../classes/UserPermissions";
import { PermissionService } from "../../../services/common/PermissionService";
import { DxDataGridComponent } from 'devextreme-angular';
import { DataGridSettings } from '../../../classes';

@Component({
    templateUrl: './settings-notifications.template.html'
})

@Injectable()
export class SettingsNotificationsComponent implements OnInit {
    static title = "Notifications";
    static path = "Notifications";
    public gridsettings = new DataGridSettings();
    @ViewChild(DxDataGridComponent, { static: false }) grid: DxDataGridComponent;
    public selectedRowKeys: Array<number> = [];
    public storageKey = 'notification-config-grid';
    public popupShow: boolean;
    public isReadOnly = true;
    public notifications: any = [];

    // notification for edit or create
    public notification: NotificationConfig;

    public get canEdit(): boolean {
        return this.selectedRowKeys.length === 1;
    }
    public get canDelete(): boolean {
        return this.grid && this.grid.instance && this.grid.instance.getSelectedRowsData().some((x: any) => !x.usedInJobs && !x.isSystem);
    }

    public columns: any[] = [
        { caption: "Name", dataField: "name" },
        { caption: "Type", dataField: "typeStr" },
        { caption: "Email", dataField: "email" },
        { caption: "Tenant", dataField: "tenantName" }
    ];

    constructor(
        private notificator: NotificationService,
        private menu: MenuService,
        private administrationService: AdministrationService,
        private permissionService: PermissionService) {

        this.notifications = administrationService.getNotificationsDataSource() || [];
    }

    public static checkPermissions(permissions: UserPermissions): boolean {
        return (permissions.IsGlobalAdmin || permissions.IsUserAdmin) &&
          (permissions.ProfileRights & CustomPermissions.Read) === CustomPermissions.Read;
    }

    ngOnInit() {
        this.menu.setFirstLevelMenu(AppRoutes.SettingsRoute);
        this.menu.setSecondLevelMenu(AppRoutes.NotificationsRoute);
        this.menu.addBreadcrumb(
          new BreadCrumb(SettingsNotificationsComponent.title, SettingsNotificationsComponent.path, 0));

        this.selectedRowKeys = [];
    }

    update() {
        this.grid.instance.refresh();
        console.log("NOI: refreshing grid...");
    }

    createNotificationConfig() {
        console.log("notifications: opening create dialog...");
        this.notification = new NotificationConfig(0);
        this.popupShow = true;
    }

    editNotificationConfig() {
        if (this.selectedRowKeys.length > 0) {
            console.log("notifications: opening edit dialog...");

            var notification = this.grid.instance.getSelectedRowsData()[0];
            this.notification = NotificationConfig.Copy(notification);
            this.popupShow = true;
        }
    }

    onNotificationConfigChanged(config: NotificationConfig) {
        this.grid.instance.refresh();
        DevExtremeHelper.scrollToRow(this.grid, config.Id);
    }

    deleteNotificationConfig() {
        this.notificator.confirmYesNo(this.doDeleteNotifications.bind(this), 'Are you sure you want to delete this notification configuration?');
    }

    doDeleteNotifications(confirmed: boolean) {
        if (!confirmed)
            return;

        var rowsToDelete = this.grid.instance.getSelectedRowsData();

        let promiseArray: Array<any> = [];
        this.notificator.setLoading(true, true);
        console.log(`notifications: deleting notifications with ids=${rowsToDelete.map((row: any) => row.id).join(',')}...`);
        promiseArray.push(this.administrationService.deleteNotification(this.selectedRowKeys));

        Promise.all(promiseArray)
            .then(() => {
                this.selectedRowKeys = [];
                this.update();
            })
            .catch(error => {
                console.log(error);
                this.notificator.showErrorNotification(Helper.GetErrorMessage(error) || 'There was an error while deleting notifications.');
                this.notificator.setLoading(false);
        });
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

    customizeColumns(columns: any) {
        for (var i = 0; i < columns.length; i++) {
            columns[i].width = "auto";
        }
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

    onNotificationChanged(config: NotificationConfig) {
        this.grid.instance.refresh();
        DevExtremeHelper.scrollToRow(this.grid, config.Id);
    }
}
