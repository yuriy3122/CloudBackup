//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, ChangeDetectorRef } from '@angular/core';

//services
import { AdministrationService } from '../../../services/administration/AdministrationService';
import { NotificationService } from '../../../services/common/NotificationService';
//helper
import { Tenant } from '../../../classes/Tenant';
import { Profile, AuthenticationType } from '../../../classes/Profile';
import { PermissionService } from '../../../services/common/PermissionService';
import { Helper } from "../../../helpers/Helper";
import { DataGridSettings } from '../../../classes';

export enum ProfileInputType {
    Name,
    Description,
    ServiceAccountId,
    KeyId,
    PrivateKey,
    Tenant
}

@Component({
    selector: 'add-edit-profile-dialog',
    templateUrl: './AddEditProfileDialog.template.html'
})
@Injectable()
export class AddEditProfileDialog implements OnInit {
    public title: string;
    public gridsettings = new DataGridSettings();
    public tenants = new Array<Tenant>();
    public selectedTenantId: number;
    public canSelectTenant = false;

    AuthenticationType = AuthenticationType; // to reference it from template

    ProfileInputType = ProfileInputType; // to reference it from template
    public errors = new Map<ProfileInputType, string>();
    public errorMessage = '';

    // profile
    @Input()
    public profile: Profile;
    @Output()
    profileChange = new EventEmitter<Profile>();

    // popup
    @Input()
    public popupVisible: boolean;
    @Output()
    popupVisibleChange = new EventEmitter<boolean>();

    constructor(
        private notificator: NotificationService,
        private administrationService: AdministrationService,
        private permissionService: PermissionService,
        private changeDetector: ChangeDetectorRef
    ) { }

    ngOnInit(): void {
        console.log('profiles: add edit dialog ngInit');
    }

    onShow() {
        // set profile-dependent data
        this.title = this.profile.Id === 0 ? "Add Account" : "Edit Account";
        this.selectedTenantId = this.profile.Id === 0 || this.profile.Tenant == null ? -1 : this.profile.Tenant.Id;

        this.changeDetector.detectChanges();

        this.errors.clear();
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

    save = async() => {
        await this.validateAllInputs();
        if (this.errors.size > 0)
            return;

        await (this.profile.Id === 0
            ? this.administrationService.addProfile(this.profile)
            : this.administrationService.updateProfile(this.profile).then(() => this.profile))
            .then(profile => {
                this.errorMessage = '';
                this.profileChange.emit(profile);
                this.hide();
            })
            .catch(error => {
                this.errorMessage = Helper.GetErrorMessage(error) || 'Unknown error in operation.';
                console.log(error);
            });
    }

    async onValueChanged(inputType: ProfileInputType): Promise<void> {
        await this.validateInput(inputType);
    }

    async validateAllInputs(): Promise<void> {
        let promiseArray = new Array<Promise<void>>();

        for (let value in ProfileInputType) {
            if (ProfileInputType.hasOwnProperty(value)) {
                let inputType = parseInt(value);

                if (inputType >= 0) {
                    promiseArray.push(this.validateInput(inputType, false));
                }
            }
        }

        await Promise.all(promiseArray);
    }

    async validateInput(inputType: ProfileInputType, noProfileCheck = false): Promise<void> {
        if (this.profile == null)
            await Promise.resolve();

        let errorMessage = '';

        switch (inputType) {
            case ProfileInputType.Name:
                if (this.profile.Name.trim() === '') {
                    errorMessage = 'Name cannot be empty.';
                }
            break;
            case ProfileInputType.ServiceAccountId:
                if (this.profile.ServiceAccountId.trim() === '') {
                    errorMessage = 'Service Account Id cannot be empty.';
                }
                break;
            case ProfileInputType.KeyId:
                if (this.profile.KeyId.trim() === '') {
                    errorMessage = 'Key Id cannot be empty.';
            }
            break;
            case ProfileInputType.Tenant:
                if (!this.canSelectTenant) break;

                this.profile.Tenant = this.tenants.find(tenant => tenant.Id === this.selectedTenantId) || null;
                break;
        }

        this.setError(inputType, errorMessage);

        this.changeDetector.detectChanges();
    }

    private setError(inputType: ProfileInputType, message: string): void {
        if (message === '')
            this.errors.delete(inputType);
        else
            this.errors.set(inputType, message);
    }
}
