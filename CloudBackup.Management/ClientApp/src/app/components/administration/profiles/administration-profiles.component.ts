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
import { CustomPermissions, UserPermissions } from "../../../classes/UserPermissions";
import { PermissionService } from "../../../services/common/PermissionService";
import { DxDataGridComponent } from 'devextreme-angular';
import { DataGridSettings } from '../../../classes';

@Component({
    templateUrl: './administration-profiles.template.html'
})

@Injectable()
export class AdministrationProfilesComponent implements OnInit {
    static title = "Accounts";
    static path = "Profiles";
    public gridsettings = new DataGridSettings();

    @ViewChild(DxDataGridComponent, { static: false }) grid: DxDataGridComponent;
    public selectedRowKeys: Array<number> = [];
    public storageKey = 'administration-profiles-grid';

    //popup visibility
    public popupShow: boolean;

    public isReadOnly = true;
    public get canEdit(): boolean {
        return this.selectedRowKeys.length === 1;
    }
    public get canDelete(): boolean {
        return this.grid && this.grid.instance && this.grid.instance.getSelectedRowsData().some((x: any) => !x.usedInJobs && !x.isSystem);
    }

    public profiles: any = [];
    // profile for edit or create
    public profile: Profile;

    public columns: any[] = [
        { caption: "Name", dataField: "name" },
        { caption: "Owner", dataField: "owner.name" },
        { caption: "Tenant", dataField: "tenant.name", name: "tenantColumn", allowSorting: false },
        { caption: "Added", dataField: "createdText", calculateSortValue: "created" }
    ];

    constructor(
        private notificator: NotificationService,
        private menu: MenuService,
        private administrationService: AdministrationService,
        private permissionService: PermissionService) {

        this.profiles = administrationService.getProfilesDataSource() || [];
    }

    public static checkPermissions(permissions: UserPermissions): boolean {
        return (permissions.IsGlobalAdmin || permissions.IsUserAdmin) &&
            (permissions.ProfileRights & CustomPermissions.Read) === CustomPermissions.Read;
    }

    ngOnInit() {
        this.menu.setFirstLevelMenu(AppRoutes.AdministrationRoute);
        this.menu.setSecondLevelMenu(AppRoutes.ProfilesRoute);
        this.menu.addBreadcrumb(
            new BreadCrumb(AdministrationProfilesComponent.title, AdministrationProfilesComponent.path, 0));

        let that = this;
        this.permissionService.getPermissions().then(permissions => {
            if (!AdministrationProfilesComponent.checkPermissions(permissions))
                alert("Access to profiles administration with insufficient rights.");

                that.isReadOnly = (permissions.ProfileRights & CustomPermissions.ReadWrite) !== CustomPermissions.ReadWrite;
                if (!permissions.IsGlobalAdmin) {
                    let tenantColumnIndex = this.columns.findIndex(x => x.name === "tenantColumn");
                    this.columns.splice(tenantColumnIndex, 1);
                }
        });

        this.selectedRowKeys = [];
    }

    update() {
        this.grid.instance.refresh();
        console.log("profiles: refreshing grid...");
    }

    createProfile() {
        console.log("profiles: opening create dialog...");
        this.profile = new Profile();
        this.popupShow = true;
    }

    editProfile() {
        if (this.selectedRowKeys.length > 0) {
            console.log("profiles: opening edit dialog...");
            var profile = this.grid.instance.getSelectedRowsData()[0];
            this.profile = Profile.Copy(profile);
            this.popupShow = true;
        }
    }

    onProfileChanged(profile: Profile) {
        this.grid.instance.refresh();
        DevExtremeHelper.scrollToRow(this.grid, profile.Id);
    }

    deleteProfiles() {
        this.notificator.confirmYesNo(this.doDeleteProfiles.bind(this), 'Are you sure you want to delete this account?');
    }

    doDeleteProfiles(confirmed: boolean) {
        if (!confirmed)
            return;

        var rowsToDelete = this.grid.instance.getSelectedRowsData().filter((x: any) => !x.usedInJobs);

        console.log(`profiles: deleting profiles with ids=${rowsToDelete.map((row: any) => row.id).join(',')}...`);

        this.notificator.setLoading(true, true);

        let promiseArray: Array<any> = [];
        for (var rowToDelete of rowsToDelete) {
            promiseArray.push(this.administrationService.deleteProfile(rowToDelete.id));
        }

        //update table after deleting all rows
        Promise.all(promiseArray)
            .then(() => {
                this.selectedRowKeys = [];
                this.update();
            })
            .catch(error => {
                console.log(error);
                this.notificator.showErrorNotification(Helper.GetErrorMessage(error) || 'There was an error while deleting accounts.');
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
