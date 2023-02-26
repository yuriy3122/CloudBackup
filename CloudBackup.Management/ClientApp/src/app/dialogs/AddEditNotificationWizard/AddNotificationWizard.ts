//inner
import { Component, Input, Output, OnInit, EventEmitter, DoCheck, ViewChild, ChangeDetectorRef } from '@angular/core';
import { DxDataGridComponent } from 'devextreme-angular';
//services
import { NotificationService } from '../../services/common/NotificationService';
import { BackupService } from '../../services/backup/BackupService';
import { AdministrationService } from "../../services/administration/AdministrationService";
//classes
import { WizardStep } from '../AddEditWizard/AddEditWizard';
import { Tenant } from "../../classes/Tenant";
import { NotificationConfig } from '../../classes/NotificationConfig';
import { NotificationDeliveryConfig } from '../../classes/NotificationDeliveryConfig';
import { DataGridSettings } from '../../classes';
import { Helper } from '../../helpers/Helper';
export enum DeliveryInputType {
  deliveryName,
  senderEmail,
  emailSmtpServer,
  emailSmtpUserName,
  emailSmtpUserPassword,
}

@Component({
    selector: 'add-edit-notification-dialog',
    templateUrl: './AddNotificationWizard.template.html'
})
export class AddNotificationWizard implements OnInit, DoCheck {
    // backups step
    public gridsettings = new DataGridSettings();
    tenants: Array<Tenant> = [];
    selectedTenantId: number;
    selectedTenantIdChange = new EventEmitter<number>();
    selectedRestoreJobObjectTab: any;
    get canSelectTenants(): boolean {
        return this.tenants != null &&
            this.tenants.length > 1;
    }

    public validationMessageText: string;

    @Input()
    public notificationConfig: NotificationConfig;

    @Input()
    public popupVisible: boolean;

    @Output()
    notificationConfigChange = new EventEmitter<NotificationConfig>();

    public title: string;
    private clicked: boolean = false;
    public stepIndex: number;
    public steps: Array<WizardStep>;

    //locker
    public lock!: boolean;

    @Output()
    public popupVisibleChange = new EventEmitter();

    private _includeTenants: any = false;
    get includeTenants() { return this._includeTenants };
    set includeTenants(value: any) { this._includeTenants = value; }

    public notificationTypes = [
          { text: "Alert", id: 0 },
          { text: "Daily summary", id: 1 }];

    public deliveryMethod: number = 0;

    public deliveryMethods = [
          { text: "SMTP", id: 0 },
          { text: "SMS", id: 1 }];

    public deliveryConfigurationId: number = 0;
    NotEmpty(val: string) {
      return val != null && val.length>0;
    }

  public deliveryConfigurations: Array<NotificationDeliveryConfig> = [];

  public deliveryName: string = "";
  public senderEmail: string = "";
  public emailSmtpServer: string = "";
  public emailSmtpPort: number = 0;
  public emailSmtpUserName: string = "";
  public emailSmtpUserPassword: string = "";

  public errors = new Map<DeliveryInputType, string>();
  public errorMessage = ''; // common error message
  public DeliveryInputType = DeliveryInputType;

    constructor(private backupService: BackupService,
        private notificator: NotificationService,
        private administrationService: AdministrationService,
        private changeDetector: ChangeDetectorRef) {
    }

    ngDoCheck(): void {
        this.popupVisibleChange.emit(this.popupVisible);
    }

    ngOnInit(): void {
        this.stepIndex = 0;
        this.validationMessageText = "";

        this.steps = [
            new WizardStep(
                () => { },
                () => this.validateCommonInfo(),
                "Name",
                "Select tenant and delivery method"),
            new WizardStep(
                () => { },
                () => this.validateDeliveryInfo(),
                "Delivery",
                "Select delivery configuration")
        ];
    }

    onShow() {
        this.stepIndex = 0;
        this.lock = false;
        this.validationMessageText = "";
        this.setTenants();
        this.title = this.notificationConfig?.Id == 0 ? "New Notification Configuration" :
          "Edit Notification Configuration";

        for (let i = 0; i < this.steps.length; i++)
          this.steps[i].IsValid = true;

        this.setDeliveryConfigurations();

        this.changeDetector.detectChanges();
    }

    onSearchValueChanged(grid: DxDataGridComponent, e: any) {
        grid.instance.searchByText(e.value);
    }

    onHiding = () => {
        this.popupVisible = false;
        this.popupVisibleChange.emit(false);
    }

    setStepIndex(step: number) {
        this.stepIndex = step || 0;
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
            this.onHiding();
    }

    hide = () => {
        this.popupVisible = false;
        this.popupVisibleChange.emit(false);
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
            const config = this.getNotificationConfig();

            if (config.Id !== 0) {
                this.administrationService.updateNotificationConfig(config)
                    .then(() => {
                        this.notificationConfigChange.emit(config);
                        this.hide();
                        this.lock = false;
                    }, (error) => {
                        console.log(error);
                        this.lock = false;
                    });
            }
            else {
                this.lock = true;
                this.administrationService.addNotificationConfig(config)
                    .then(config => {
                        this.notificationConfigChange.emit(config);
                        this.hide();
                        this.lock = false;
                    }, (error) => {
                        console.log(error);
                        this.lock = false;
                    });
            }
        }
    }

    validateJobWizardData() {
        this.validateCommonInfo(true);
        this.validateDeliveryInfo(true);
    }

    setTenants() {
        return this.administrationService.getAllowedTenants()
            .then(tenants => {
                this.tenants = tenants;

                if (this.tenants.length > 0) {
                    this.selectedTenantId = this.tenants[0].Id;
                    this.selectedTenantIdChange.emit(this.selectedTenantId);
                }
            });
    }

    setDeliveryConfigurations() {
        this.backupService.getNotificationDeliveryConfigurations(this.selectedTenantId)
            .then(configurations => {
                this.deliveryConfigurations = new Array<NotificationDeliveryConfig>();
                this.deliveryConfigurations.push(NotificationDeliveryConfig.Copy({ Id: 0, Name: 'Default' }));
                this.deliveryConfigurations.push(...configurations.map(x => Object.assign({}, x)));

                // reset values to default
                this.deliveryName = this.senderEmail = this.emailSmtpServer = 
                this.emailSmtpUserName = this.emailSmtpUserPassword = "";
                this.emailSmtpPort = 0;

                // set current values
                this.deliveryConfigurationId = this.notificationConfig &&
                this.notificationConfig.DeliveryConfig != null ?
                this.notificationConfig.DeliveryConfig.Id : 0;
                this.setDeliveryConfigurationParams(this.deliveryConfigurationId);
        });
    }

    setDeliveryConfigurationParams(id: number) {
        let deliveryConfiguration = this.deliveryConfigurations.find(x => x.Id === id);

        if (deliveryConfiguration) {
            this.deliveryName = (deliveryConfiguration.Id === 0 ? '' : deliveryConfiguration.Name) || "";
            this.senderEmail = deliveryConfiguration.SenderEmail || "";
            this.emailSmtpServer = deliveryConfiguration.EmailSmtpServer || "";
            this.emailSmtpPort = deliveryConfiguration.EmailSmtpPort || 0;
            this.emailSmtpUserName = deliveryConfiguration.EmailSmtpUserName || "";
            this.emailSmtpUserPassword = deliveryConfiguration.EmailSmtpUserPassword || "";
        }
     
  }
  
  onValueChanged = async (inputType: DeliveryInputType): Promise<void> => {
    await this.validateInput(inputType);
  }
  validateAllInputs = async (): Promise<void> => {
    let promiseArray = new Array<Promise<void>>();
    for (let value in DeliveryInputType) {
      if (DeliveryInputType.hasOwnProperty(value)) {
        let inputType = parseInt(value);
        if (inputType >= 0)
          promiseArray.push(this.validateInput(inputType));
      }
    }
    await Promise.all(promiseArray);
  }
  isEmpty(val: string) {
    return val == null || val.length == 0;
  }
  public errorEmail = 'Email is not correct.';
  validateInput = async (inputType: DeliveryInputType): Promise<void> => {

    let errorMessage = '';

      switch (inputType) {
        case DeliveryInputType.deliveryName:
          if (this.deliveryName.trim() === '')
            errorMessage = 'Name cannot be empty.';
          break;

        case DeliveryInputType.senderEmail:
          if (!Helper.EmailRegex.test(this.senderEmail))
            errorMessage = 'Email is not correct.';
          break;
        case DeliveryInputType.emailSmtpServer:
          if (this.emailSmtpServer.trim() === '')
            errorMessage = 'Email smtp server cannot be empty.';
          break;
        case DeliveryInputType.emailSmtpUserName:
          if (this.emailSmtpUserName.trim() === '')
            errorMessage = 'Email smtp server user name cannot be empty.';
          break;
        case DeliveryInputType.emailSmtpUserPassword:
          if (this.emailSmtpUserPassword.trim() === '')
            errorMessage = 'Email smtp server user password cannot be empty.';
          break;
      }

      if (errorMessage === '')
        this.errors.delete(inputType);
      else
        this.errors.set(inputType, errorMessage);

      this.changeDetector.detectChanges();
    }

    onTenantValueChanged(e: any) {
        this.setDeliveryConfigurations();
    }

    public validateCommonInfo(updateStep = true): Boolean {
        if (!this.notificationConfig)
            return false;

      const isValid = this.notificationConfig.isNameValid && this.notificationConfig.isEmailValid;

        if (updateStep)
            this.steps[0].IsValid = isValid;

        return isValid;
    }

    public validateDeliveryInfo(updateStep = true): Boolean {
        if (!this.notificationConfig)
            return false;
      this.validateAllInputs();
      let isValid = this.errors.size==0;
      if (updateStep)
        this.steps[1].IsValid = isValid;
        return isValid;
    }

    getSelectedDeliveryConfiguration(): NotificationDeliveryConfig {
        var deliveryConfiguration = new NotificationDeliveryConfig(this.deliveryConfigurationId);
        deliveryConfiguration.Name = this.deliveryName || "";
        deliveryConfiguration.TenantId = this.selectedTenantId;
        deliveryConfiguration.SenderEmail = this.senderEmail;
        deliveryConfiguration.EmailSmtpServer = this.emailSmtpServer;
        deliveryConfiguration.EmailSmtpPort = this.emailSmtpPort;
        deliveryConfiguration.EmailSmtpUserName = this.emailSmtpUserName;
        deliveryConfiguration.EmailSmtpUserPassword = this.emailSmtpUserPassword;

        return deliveryConfiguration;
    }

    public getNotificationConfig() {
        const notificationConfig = NotificationConfig.Copy(this.notificationConfig);
        notificationConfig.TenantId = this.selectedTenantId;
        notificationConfig.IncludeTenants = this.includeTenants;
        notificationConfig.DeliveryConfig = this.getSelectedDeliveryConfiguration();
        return notificationConfig;
    }

    onSelectedDeliveryConfigurationChanged(e: any) {
        if (e != null) {
            this.setDeliveryConfigurationParams(this.deliveryConfigurationId);
        }
    }
}
