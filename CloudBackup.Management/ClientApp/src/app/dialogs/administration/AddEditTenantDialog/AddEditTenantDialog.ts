//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, ChangeDetectorRef } from '@angular/core';

//services
import { AdministrationService } from '../../../services/administration/AdministrationService';
import { NotificationService } from '../../../services/common/NotificationService';
//helper
import { Tenant } from '../../../classes/Tenant';
import { PermissionService } from '../../../services/common/PermissionService';
import { Helper } from "../../../helpers/Helper";
import { DataGridSettings } from '../../../classes';

@Component({
    selector: 'add-edit-tenant-dialog',
    templateUrl: './AddEditTenantDialog.template.html'
})
@Injectable()
export class AddEditTenantDialog implements OnInit {
    public title: string;
    public errorMessage = '';
    public nameErrorMessage = '';
    public gridsettings = new DataGridSettings();
    // tenant
    @Input()
    public tenant: Tenant;
    @Output()
    tenantChange = new EventEmitter<Tenant>();

    // popup
    @Input()
    public popupVisible: boolean;
    @Output()
    popupVisibleChange = new EventEmitter<boolean>();

    constructor(
        private administrationService: AdministrationService,
        private changeDetector: ChangeDetectorRef
    )
    {}

    ngOnInit(): void {
        console.log('tenants: add edit dialog ngInit');
    }

    onShow() {
        this.title = this.tenant.Id === 0 ? "Add Tenant" : "Edit Tenant";

        this.changeDetector.detectChanges();

        this.errorMessage = '';

        this.popupVisibleChange.emit(this.popupVisible);
    }

    hide = () => {
        this.popupVisible = false;
        this.changeDetector.detectChanges();
    }

    onHiding() {
        this.popupVisibleChange.emit(false);
    }

    save = async () => {
        this.validateName();

        if (this.nameErrorMessage.length > 0)
          return;

        await (this.tenant.Id === 0
          ? this.administrationService.addTenant(this.tenant)
          : this.administrationService.updateTenant(this.tenant).then(() => this.tenant))
            .then(tenant => {
                this.errorMessage = '';
                this.tenantChange.emit(tenant);
                this.hide();
            })
            .catch(error => {
                this.errorMessage = Helper.GetErrorMessage(error) || 'Unknown error in operation.';
                console.log(error);
            });
    }

    get isTenantIsolated() {
        return this.tenant == null ? false : this.tenant.Isolated;
    };

    set isTenantIsolated(value: any) {
        if (this.tenant != null)
          this.tenant.Isolated = value;
    }

    private validateName(): void {
        if (this.tenant == null)
            return;

        this.nameErrorMessage = '';

        if (this.tenant.Name.trim() === '')
            this.nameErrorMessage = 'Name cannot be empty.';

        this.changeDetector.detectChanges();
    }

    onNameChanged(): void {
      this.validateName();
    }
}
