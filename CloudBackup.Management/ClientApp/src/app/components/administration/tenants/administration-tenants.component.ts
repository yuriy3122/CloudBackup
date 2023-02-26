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
import { Tenant } from '../../../classes/Tenant';
import { PermissionService } from '../../../services/common/PermissionService';
import { DataGridSettings } from '../../../classes';

@Component({
    templateUrl: './administration-tenants.template.html'
})

@Injectable()
export class AdministrationTenantsComponent implements OnInit {
    static title = "Tenants";
    static path = "Tenants";
    public gridsettings = new DataGridSettings();

    @ViewChild(DxDataGridComponent, { static: false }) grid: DxDataGridComponent;
    public selectedRowKeys: Array<number> = [];
    public storageKey = 'administration-tenants-grid';

    //popup visibility
    public popupShow: boolean;

    public isReadOnly = true;
    public get canEdit(): boolean {
        return this.selectedRowKeys.length === 1;
    }
    public get canDelete(): boolean {
        return this.grid && this.grid.instance && this.grid.instance.getSelectedRowsData().some((x: any) => !x.isSystem);
    }

    public tenants: any = [];
    public tenant: Tenant;

    public columns: any[] = [
      { caption: "Name", dataField: "name" },
      { caption: "Description", dataField: "description" },
      { caption: "Isolated", dataField: "isolated", calculateCellValue: (row: any) => row.isolated ? 'Yes' : 'No' },
      { caption: "Administrators", calculateCellValue: this.getAdminsText, width: '30%' }
    ];

    constructor(
        private notificator: NotificationService,
        private menu: MenuService,
        private administrationService: AdministrationService,
        private permissionService: PermissionService) {

        this.tenants = administrationService.getTenantsDataSource() || [];
    }

    public static checkPermissions(permissions: UserPermissions): boolean {
        return (permissions.IsGlobalAdmin || permissions.IsUserAdmin) &&
          (permissions.TenantRights & CustomPermissions.Read) === CustomPermissions.Read;
    }

    ngOnInit() {
      this.menu.setFirstLevelMenu(AppRoutes.AdministrationRoute);
      this.menu.setSecondLevelMenu(AppRoutes.TenantsRoute);
        this.menu.addBreadcrumb(
            new BreadCrumb(AdministrationTenantsComponent.title, AdministrationTenantsComponent.path, 0));

        let that = this;
        this.permissionService.getPermissions().then(permissions => {
            if (!AdministrationTenantsComponent.checkPermissions(permissions))
                alert("Access to profiles administration with insufficient rights.");

            that.isReadOnly = (permissions.TenantRights & CustomPermissions.ReadWrite) !== CustomPermissions.ReadWrite;
        });

        this.selectedRowKeys = [];
    }

    getAdminsText(rowData: any) {
      return rowData.admins.map((x: any) => x.name).join('; ');
    }

    update() {
        this.grid.instance.refresh();
        console.log("tenants: refreshing grid...");
    }

    createTenant() {
        console.log("tenants: opening create dialog...");
        this.tenant = new Tenant();
        this.popupShow = true;
    }

    editTenant() {
          if (this.selectedRowKeys.length > 0) {
              console.log("tenants: opening edit dialog...");
            var tenant = this.grid.instance.getSelectedRowsData()[0];
            this.tenant = Tenant.Copy(tenant);
            this.popupShow = true;
        }
    }

    onTenantChanged(tenant: Tenant) {
        this.grid.instance.refresh();
        DevExtremeHelper.scrollToRow(this.grid, tenant.Id);
    }

    deleteTenants() {
        this.notificator.confirmYesNo(this.doDeleteTenants.bind(this), 'Are you sure you want to delete this tenant');
    }

    doDeleteTenants(confirmed: boolean) {
        if (!confirmed)
            return;

        var rowsToDelete = this.grid.instance.getSelectedRowsData().filter((x: any) => x.canDelete);

        console.log(`tenants: deleting tenants with ids=${rowsToDelete.map((row: any) => row.id).join(',')}...`);

        this.notificator.setLoading(true, true);

        let promiseArray: Array<any> = [];
        for (var rowToDelete of rowsToDelete) {
            promiseArray.push(this.administrationService.deleteTenant(rowToDelete.id));
        }

        //update table after deleting all rows
        Promise.all(promiseArray)
            .then(() => {
                this.selectedRowKeys = [];
                this.update();
            })
            .catch(error => {
                console.log(error);
                this.notificator.showErrorNotification(Helper.GetErrorMessage(error) || 'There was an error while deleting tenants.');
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
