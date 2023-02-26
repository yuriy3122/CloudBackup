//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, ChangeDetectorRef } from '@angular/core';

//services
import { AdministrationService } from '../../../services/administration/AdministrationService';
import { NotificationService } from '../../../services/common/NotificationService';
//helper
import { Tenant } from '../../../classes/Tenant';
import { PermissionService } from '../../../services/common/PermissionService';
import { Helper } from "../../../helpers/Helper";
import { Role } from '../../../classes/Role';
import { UtcOffset } from '../../../classes/UtcOffset';
import { User } from '../../../classes/User';
import { CustomPermissions } from '../../../classes/UserPermissions';
import { DataGridSettings } from '../../../classes';

export enum UserInputType {
  Name,
  Description,
  Login,
  Password,
  ConfirmPassword,
  Email,
  UtcOffset,
  Role,
  Tenant
}

@Component({
  selector: 'add-edit-user-dialog',
  providers: [],
  templateUrl: './AddEditUserDialog.template.html'
})
@Injectable()
export class AddEditUserDialog implements OnInit {
  public title: string;
  public gridsettings = new DataGridSettings();
  public tenants: Tenant[];
  public selectedTenantId: number;

  public roles: Role[];
  public selectedRoleId: number;

  public utcOffsets: UtcOffset[];
  public selectedUtcOffsetTotalMinutes: number;

  public initialLogin: string = '';
  public get loginChanged() {
    return this.user != null && this.user.Login !== this.initialLogin;
  };

  public passwordMock = '12345678'; // Just to simulate entered password (because we don't know it).
  public password: string;
  public confirmPassword: string;
  // Flags if password or confirmation was changed by user. Password wouldn't be saved if both were not explicitly set by user.
  // This is needed to avoid situation where password is equal to passwordMock.
  public passwordChanged = false;
  public confirmPasswordChanged = false;

  public UserInputType = UserInputType; // to reference it from template
  public errors = new Map<UserInputType, string>();
  public errorMessage = ''; // common error message

  public isGlobalAdmin = false;
  public canSelectRoles = false;
  public canSelectTenant = false;

  // user
  @Input()
  public user: User;
  @Output()
  userChange = new EventEmitter<User>();

  // popup
  @Input()
  public popupVisible: boolean;
  @Output()
  popupVisibleChange = new EventEmitter<boolean>();

  constructor(
    private notificator: NotificationService,
    private administrationService: AdministrationService,
    private permissionService: PermissionService,
    private changeDetector: ChangeDetectorRef) {
  }

  ngOnInit(): void {
    console.log('users: add edit dialog ngInit');

    // set data
    let that = this;
    this.administrationService.getUtcOffsets().then(offsets => this.utcOffsets = offsets);
    this.administrationService.getRoles().then(roles => this.roles = roles);

    this.permissionService.getPermissions().then(permissions => {
      // only users with TenantRights.Read can change tenants
      if (permissions.IsGlobalAdmin && (permissions.TenantRights & CustomPermissions.Read) === CustomPermissions.Read) {
        that.canSelectTenant = true;
      } else {
        that.tenants = [];
        that.canSelectTenant = false;
      }

      // only global admins can select global admin role
      that.isGlobalAdmin = permissions.IsGlobalAdmin;
    });
  }

  onShow() {
    // set user-dependent data
    this.title = this.user.Id === 0 ? "Add User" : "Edit User";
    this.initialLogin = this.user.Login;
    this.selectedTenantId = this.user.Id === 0 ? -1 : this.user.Tenant.Id;
    this.selectedRoleId = this.user.Id === 0 ? -1 : this.user.Role.Id;
    this.selectedUtcOffsetTotalMinutes = this.user.Id === 0 ? -(new Date().getTimezoneOffset()) : this.user.UtcOffset.Offset.TotalMinutes;
    this.password = this.confirmPassword = this.user.Id === 0 ? '' : this.passwordMock;
    this.passwordChanged = this.confirmPasswordChanged = false;

    // if edited user has global admin role and user is not global admin, then he can't remove that role.
    this.canSelectRoles = this.user.Id === 0 || !(this.user.Role.IsGlobalAdmin && !this.isGlobalAdmin);

    this.changeDetector.detectChanges();

    // reset state to default after bindings initialization
    this.passwordChanged = this.confirmPasswordChanged = this.user.Id === 0; // disables password mocking in add mode
    this.errors.clear();
    this.errorMessage = '';

    this.setTenants();

    this.changeDetector.detectChanges();

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
    await this.validateAllInputs();
    if (this.errors.size > 0)
      return;

    await (this.user.Id === 0
      ? this.administrationService.addUser(this.user)
      : this.administrationService.updateUser(this.user).then(() => this.user))
      .then(user => {
        this.errorMessage = '';
        this.userChange.emit(user);
        this.hide();
      })
      .catch(error => {
        this.errorMessage = Helper.GetErrorMessage(error) || 'Unknown error in operation.';
        console.log(error);
      });
  }

  setTenants() {
    return this.administrationService.getAllowedTenants()
      .then(tenants => {
        this.tenants = tenants;

        if (this.tenants.length > 0) {
          this.selectedTenantId = this.tenants[0].Id;
        }
      });
  }

  get isUserEnabled() {
    return this.user == null ? false : this.user.IsEnabled;
  };

  set isUserEnabled(value: any) {
    if (this.user != null)
      this.user.IsEnabled = value;
  }

  onPasswordInput(): void {
    this.passwordChanged = true;
  }
  onConfirmPasswordInput(): void {
    this.confirmPasswordChanged = true;
  }
  // We clear password inputs on focus to make sure the password is overridden
  onPasswordFocusIn() {
    if (!this.passwordChanged)
      this.password = "";
  }
  onConfirmPasswordFocusIn() {
    if (!this.confirmPasswordChanged)
      this.confirmPassword = "";
  }
  // We restore mock password if user didn't modify the data.
  onPasswordFocusOut() {
    if (!this.passwordChanged)
      this.password = this.passwordMock;
  }
  onConfirmPasswordFocusOut() {
    if (!this.confirmPasswordChanged)
      this.confirmPassword = this.passwordMock;
  }

  onValueChanged = async (inputType: UserInputType): Promise<void> => {
    switch (inputType) {
      case UserInputType.Tenant:
        if (this.canSelectTenant)
          if (this.tenants)
            this.user.Tenant = this.tenants.find(tenant => tenant.Id === this.selectedTenantId)!;
        break;
      case UserInputType.Role:
        this.user.Role = this.roles.find(role => role.Id === this.selectedRoleId)!;
        if (this.user.Role != null && this.user.Role.IsGlobalAdmin && this.canSelectTenant) {
          // If global admin role is selected, reset tenant selection to the system tenant.

          if (this.tenants)
            this.user.Tenant = this.tenants.find(tenant => tenant.IsSystem)!;

          this.selectedTenantId = this.user.Tenant?.Id || -1;

          await this.validateInput(UserInputType.Tenant);
        }
        break;
    }

    await this.validateInput(inputType);
  }

  validateAllInputs = async (): Promise<void> => {
    let promiseArray = new Array<Promise<void>>();

    for (let value in UserInputType) {
      if (UserInputType.hasOwnProperty(value)) {
        let inputType = parseInt(value);
        if (inputType >= 0)
          promiseArray.push(this.validateInput(inputType));
      }
    }

    await Promise.all(promiseArray);
  }

  validateInput = async (inputType: UserInputType): Promise<void> => {
    if (this.user == null)
      return;

    let errorMessage = '';

    switch (inputType) {
      case UserInputType.Name:
        if (this.user.Name.trim() === '')
          errorMessage = 'Name cannot be empty.';
        break;
      case UserInputType.Login:
        if (this.user.Login.trim() === '')
          errorMessage = 'Login cannot be empty.';
        else if (!Helper.AlphaNumericNotStartsWithNumberRegex.test(this.user.Login))
          errorMessage = 'Login must contain only latin characters, numbers or underscores.';
        else if (this.loginChanged) {
          if (this.user.Id !== 0) {
            errorMessage = "Can't edit login.";
          } else {
            var loginExists = await this.administrationService.checkLoginAsync(this.user.Login);
            if (loginExists)
              errorMessage = 'This login is already used by another user.';
          }
        }
        break;
      case UserInputType.Email:
        if (!Helper.EmailRegex.test(this.user.Email))
          errorMessage = 'Email is not correct.';
        break;
      case UserInputType.Password:
        if (this.passwordChanged) {
          if (this.password.trim() === '')
            errorMessage = 'Password cannot be empty.';
          else if (!Helper.PasswordRegex.test(this.password))
            errorMessage = 'Password must contain only latin characters, numbers or special symbols.';

          // password equality might have changed - and it is checked in UserInputType.ConfirmPassword
          this.validateInput(UserInputType.ConfirmPassword);
        }
        break;
      case UserInputType.ConfirmPassword:
        if (this.confirmPassword !== '' && // because empty password is handled in UserInputType.Password
          (this.password !== this.confirmPassword || (this.passwordChanged !== this.confirmPasswordChanged)))
          errorMessage = 'Two passwords are different.';
        break;
      case UserInputType.Tenant:
        if (!this.canSelectTenant) break;

        if (this.user.Tenant == null)
          errorMessage = 'A tenant must be selected.';
        else if (!this.user.Tenant.IsSystem && this.user.Role != null && this.user.Role.IsGlobalAdmin)
          errorMessage = 'A member of global admin role must be a part of a system tenant.';
        break;
      case UserInputType.Role:
        if (this.user.Role == null)
          errorMessage = 'A role must be selected.';
        else if (this.user.Role.IsGlobalAdmin && this.user.Tenant != null && !this.user.Tenant.IsSystem)
          errorMessage = 'A member of global admin role must be a part of a system tenant.';
        break;
      case UserInputType.UtcOffset:
        this.user.UtcOffset = this.utcOffsets.find(offset => offset.Offset.TotalMinutes === this.selectedUtcOffsetTotalMinutes)!;
        if (this.user.UtcOffset == null)
          errorMessage = 'UTC offset must be selected.';
        break;
    }

    if (inputType === UserInputType.Password || inputType === UserInputType.ConfirmPassword) {
      // save password only if it was set by user
      if (this.passwordChanged && this.confirmPasswordChanged)
        this.user.Password = this.password;
    }

    if (errorMessage === '')
      this.errors.delete(inputType);
    else
      this.errors.set(inputType, errorMessage);

    this.changeDetector.detectChanges();
  }
}
