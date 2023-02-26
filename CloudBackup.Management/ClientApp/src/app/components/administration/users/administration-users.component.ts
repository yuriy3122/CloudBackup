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
import { CustomPermissions, UserPermissions } from "../../../classes/UserPermissions";
import { DxDataGridComponent } from 'devextreme-angular';
import { User } from '../../../classes/User';
import { PermissionService } from '../../../services/common/PermissionService';
import { DataGridSettings } from '../../../classes';

@Component({
    templateUrl: './administration-users.template.html'
})

@Injectable()
export class AdministrationUsersComponent implements OnInit {
    static title = "Users";
    static path = "Users";
    public gridsettings = new DataGridSettings();

    @ViewChild(DxDataGridComponent) grid: DxDataGridComponent;
    public selectedRowKeys: Array<number>;
    public storageKey = 'administration-users-grid';

    //popup visibility
    public popupShow: boolean;

    public isReadOnly = true;
    public get canEdit(): boolean {
        return this.selectedRowKeys.length === 1;
    }
    public get canDelete(): boolean {
        return this.grid && this.grid.instance && this.grid.instance.getSelectedRowsData().some((x: any) => !x.isSystem);
    }

    public users: any = [];
    //user for edit or create
    public user: User;

    public columns: any[] = [
        { caption: "Login", dataField: "login" },
        { caption: "Name", dataField: "name" },
        { caption: "Email", dataField: "email" },
        { caption: "UTC Offset", dataField: "utcOffset.name" },
        { caption: "Tenant", name: "tenantColumn", dataField: "tenant.name" },
        { caption: "Role", dataField: "role.name", allowSorting: false },
        { caption: "Enabled", dataField: "isEnabled",width:"100" },
        { caption: "Description", dataField: "description" }
    ];

    constructor(
        private notificator: NotificationService,
        private menu: MenuService,
        private administrationService: AdministrationService,
        private permissionService: PermissionService) {
        this.users = administrationService.getUsersDataSource() || [];
    }

    public static checkPermissions(permissions: UserPermissions): boolean {
        return (permissions.IsGlobalAdmin || permissions.IsUserAdmin) &&
            (permissions.UserRights & CustomPermissions.Read) === CustomPermissions.Read;
    }

    ngOnInit() {
        this.menu.setFirstLevelMenu(AppRoutes.AdministrationRoute);
        this.menu.setSecondLevelMenu(AppRoutes.UsersRoute);
        this.menu.addBreadcrumb(
            new BreadCrumb(AdministrationUsersComponent.title, AdministrationUsersComponent.path, 0));

        let that = this;
        this.permissionService.getPermissions().then(permissions => {
            if (!AdministrationUsersComponent.checkPermissions(permissions))
                alert("Access to profiles administration with insufficient rights.");

            that.isReadOnly = (permissions.UserRights & CustomPermissions.ReadWrite) !== CustomPermissions.ReadWrite;
            if (!permissions.IsGlobalAdmin) {
                let tenantColumnIndex = this.columns.findIndex(x => x.name === "tenantColumn");
                this.columns.splice(tenantColumnIndex, 1);
            }
        });
        this.selectedRowKeys = [];
    }

    update() {
        this.grid.instance.refresh();
        console.log("users: refreshing grid...");
    }

    createUser() {
        console.log("users: opening create dialog...");
        this.user = new User();
        this.popupShow = true;
    }

    editUser() {
        if (this.selectedRowKeys.length > 0) {
            console.log("users: opening edit dialog...");
            var user = this.grid.instance.getSelectedRowsData()[0];
            this.user = User.Copy(user);
            this.popupShow = true;
        }
    }

    onUserChanged(user: User) {
        this.grid.instance.refresh();
        DevExtremeHelper.scrollToRow(this.grid, user.Id);
    }

    deleteUsers() {
        this.notificator.confirmYesNo(this.doDeleteUsers.bind(this), 'Are you sure you want to delete this user');
    }

    doDeleteUsers(confirmed: boolean) {
        if (!confirmed)
            return;

        var rowsToDelete = this.grid.instance.getSelectedRowsData().filter((x: any) => x.canDelete);

        this.notificator.setLoading(true, true);

        let promiseArray: Array<any> = [];
        for (var rowToDelete of rowsToDelete) {
            promiseArray.push(this.administrationService.deleteUser(rowToDelete.id));
        }

        Promise.all(promiseArray)
            .then(() => {
                this.selectedRowKeys = [];
                this.update();
            })
            .catch(error => {
                console.log(error);
                this.notificator.showErrorNotification(Helper.GetErrorMessage(error) || 'There was an error while deleting users.');
                this.notificator.setLoading(false);
            });
    }

    onSearchValueChanged(e: any) {
        this.grid.instance.searchByText(e.value);
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
}
