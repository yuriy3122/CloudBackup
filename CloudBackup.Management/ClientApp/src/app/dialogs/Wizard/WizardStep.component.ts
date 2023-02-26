import { Component, Injectable, HostBinding, EventEmitter, Input, Output } from "@angular/core";

@Component({
    selector: 'wizard-step',
    host: {
        'class': 'wizard-step-content',
        '[style.display]': '"block"'
    },
    template: `<ng-content></ng-content>`
})
@Injectable()
export class WizardStepComponent {
    @Input()
    header!: string;
    @Input()
    description!: string;
    @Input()
    subDescription!: string;

    isCurrent = false;
    @HostBinding('class.hide')
    get isHidden() : boolean {
        return !this.isCurrent;
    }

    @Input()
    isValid: boolean | (() => boolean);

    @Output()
    selected = new EventEmitter();

    constructor() {
        this.isValid = true;
    }

    setCurrent(value: boolean) {
        if (value === this.isCurrent)
            return;

        this.isCurrent = value;

        if (value)
            this.selected.emit();
    }

    checkValid() {
        if (typeof this.isValid === 'boolean') {
            return this.isValid as boolean;
        }
        else if (this.isValid instanceof Function) {
            return this.isValid();
        }
        return false;
    }
}