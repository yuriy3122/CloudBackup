//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, DoCheck, ViewChild, ChangeDetectorRef } from '@angular/core';
//services
import { NotificationService } from "../../services/common/NotificationService";
import { Configuration } from 'src/app/classes/Configuration';
import { Helper } from 'src/app/helpers/Helper';
import { WizardStep } from '../AddEditWizard/AddEditWizard';
import { UtcOffset } from '../../classes/UtcOffset';
import { Router } from '@angular/router';
import { AdministrationService } from '../../services/administration/AdministrationService';
import { AuthService } from 'src/app/services/auth/AuthService';

export enum RegistrationInputType {
    InstanceId = 1,
    Password,
    Email,
    FullName,
    UserName
}

@Component({
    selector: 'registration-wizard',
    providers: [],
    templateUrl: './RegistrationWizard.template.html',
   
})

@Injectable()
export class RegistrationWizard implements OnInit, DoCheck {
    //static data
    static Path: "Registration";
    static Title: "Registration";
    //basic data
    buttonNextText = "Next";
    buttonCancelText = "Cancel";
    buttonBackText = "Back";
    buttonEndText = "Finish";
    //locker
    clicked: boolean = false;
    //wizard data
    //steps for wizard
    public Steps!: Array<WizardStep>;
    //wizard options
    //step number
    public stepIndex: number;
    //show popup or not
    @Input()
    popupVisible: boolean;
    @Output() popupVisibleChange = new EventEmitter();
    //configuration is always new
    public configuration: Configuration;
    //errors 
    public errors: any;
    public errorMessages: any;

    public RegistrationInputType = RegistrationInputType;
    //password repeat
    public password: string;

    //locker
    lock: boolean;

    //title
    public title: string = "Registration";

    public licenseDisabled: boolean = false;
    //public licenseAccepted: boolean = false;
    public licenseAcceptanceValid: boolean = false;
    public errorMessage: string = "";

    private _licenseAccepted: any = false;
    get licenseAccepted() { return this._licenseAccepted };
    set licenseAccepted(value: any) { this._licenseAccepted = value; }

    public serverInstanceId: string;
    public loginPattern: any;
    public passwordPattern: any;

    //time zones
    public utcOffsets: Array<UtcOffset>;

    constructor(private notificator: NotificationService,
        private router: Router,
        private authService: AuthService,
        private administrationService: AdministrationService) {
        this.configuration = new Configuration();
        this.errors = {
            InstanceId: false,
            Password: false,
            Email: false,
            UserName: false
        };
        this.errorMessages = {
            InstanceId: "",
            Password: "",
            Email: "",
            UserName: ""
        };
        this.password = "";
        this.licenseAccepted = this.licenseAcceptanceValid = false;
        this.loginPattern = Helper.AlphaNumericNotStartsWithNumberRegex;
        this.passwordPattern = Helper.PasswordRegex;

        this.administrationService.getUtcOffsets()
            .then(result => {
                this.utcOffsets = (result || []).map((x: any) => UtcOffset.Copy(x));
                var currentOffset = this.utcOffsets.find(x => x.Offset.TotalMinutes === -(new Date().getTimezoneOffset()));
                if (currentOffset != null)
                    this.configuration.UtcOffset = currentOffset.Offset;
            }).catch(error => console.log(error));

        this.licenseDisabled = true;
    }

    ngDoCheck() {
        this.popupVisibleChange.emit(this.popupVisible);
    }

    ngOnInit() {
        let that = this;
        this.stepIndex = 0;
        this.lock = false;

        that.Steps = [
            new WizardStep(
                function () { },
                function () {
                    var result = that.onInstanceIdChanged();
                    result = that.onLicenseAcceptedChanged() && result;
                    return result;
                },
                "STEP 1",
                "To proceed, please type the  Backup instance ID"),
            new WizardStep(
                function () { },
                function () {
                    var result = that.onInputChanged(RegistrationInputType.UserName);
                    result = that.onPasswordChanged() && result;
                    return result;
                },
                "STEP 2",
                "Please specify root administrator credentials"),
            new WizardStep(
                function () { },
                function () {
                    return true;
                },
                "STEP 3",
                "Define a time zone")
        ];
    }

    onShow() {
        this.clicked = false;
        let that = this;
    }

    onHiding() {
        this.popupVisible = false;
        this.popupVisibleChange.emit(false);
    }

    nextStep = () => {
        this.errorMessage = "";
        let stepResult = this.Steps[this.stepIndex].OnNextStep();
        if (stepResult && this.Steps[this.stepIndex].IsValid) {
            if (this.stepIndex < this.Steps.length) {
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
            this.hide();
    }


    onComplete = () => {
        let that = this;

        if (that.lock) {
            return;
        }

        that.lock = true;

        let stringed = JSON.stringify(this.configuration);

        this.authService.setConfiguration(stringed).then(() => {
            that.stepIndex++;
            //wait while data is being processed
            var interval = setInterval(function () {
                that.authService.isConfigured()
                    .then(function (result) {
                        if (result.isConfigured || result.IsConfigured) {
                            that.router.navigate(['/Login']);
                            that.lock = false;
                            clearInterval(interval);
                        } else {
                            var error = result.errorMessage || result.ErrorMessage;

                            if (error != null && error.length > 0) {
                                that.errorMessage = error;
                                that.lock = false;
                                that.setStepIndex(that.Steps.length - 2);
                                clearInterval(interval);
                            }
                        }
                    }, error => {
                        that.lock = false;
                        console.log(error);
                    });
            }, 5000);
        }, failed => {
            that.lock = false;
            if (failed.error && failed.error.error) {
                const errorMessage = failed.error.error;
                that.errorMessage = errorMessage;
                console.log(failed.error.error);
            }
        });
    }

    setStepIndex(step: number) {
        this.stepIndex = step || 0;
    }

    hide() {
        this.popupVisible = false;
        this.popupVisibleChange.emit(false);
    }

    onLicenseAcceptedChanged() {
        this.licenseAcceptanceValid = this.licenseAccepted;
        return this.licenseAcceptanceValid;
    }

    onInputChanged(type: RegistrationInputType) {
        //if (this.configuration[RegistrationInputType[type]] == '') {
        //    this.errors[type] = true;
        //    this.errorMessages[type] = "Error";
        //    return false;
        //}
        this.errors[type] = false;
        this.errorMessages[type] = "";
        return true;
    }

    onInstanceIdChanged() {
        if (!this.configuration.InstanceId) {
            this.errors[RegistrationInputType.InstanceId] = true;
            this.errorMessages[RegistrationInputType.InstanceId] = " Backup instance ID is not set";
            return false;
        }
        else if (this.serverInstanceId && this.configuration.InstanceId !== this.serverInstanceId) {
            this.errors[RegistrationInputType.InstanceId] = true;
            this.errorMessages[RegistrationInputType.InstanceId] = " Backup instance ID does not match current instance ID";
            return false;
        }

        this.errors[RegistrationInputType.InstanceId] = false;
        this.errorMessages[RegistrationInputType.InstanceId] = "";
        return true;
    }

    onPasswordChanged() {
        if (this.configuration.Password != this.password) {
            this.errors[RegistrationInputType.Password] = true;
            this.errorMessages[RegistrationInputType.Password] = "Error";
            return false;
        } else {
            this.errors[RegistrationInputType.Password] = false;
            this.errorMessages[RegistrationInputType.Password] = "";
        }
        return true;
    }
}
