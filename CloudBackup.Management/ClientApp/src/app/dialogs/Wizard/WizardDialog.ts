import { Component, Injectable, HostBinding, ContentChildren, QueryList, EventEmitter, Input, Output, ChangeDetectorRef, OnInit, ViewChild, SimpleChanges } from "@angular/core";
import { DataGridSettings } from "../../classes";
import { WizardStepComponent } from './WizardStep.component';

@Component({
    selector: 'wizard-dialog',

    templateUrl: './WizardDialog.template.html'
})
@Injectable()
export class WizardDialog implements OnInit {
    @Input()
    title!: string;
   
    // popup
    @Input()
    visible!: boolean;
    @Output() visibleChange = new EventEmitter<boolean>();

    @Output() shown = new EventEmitter();
    @Output() hidden = new EventEmitter();

    @Input() width: any = '80%';
    @Output() widthChange = new EventEmitter<any>();
    @Input() height: any = '80%';
    @Output() heightChange = new EventEmitter<any>();

    // wizard logic
    @ContentChildren(WizardStepComponent)
    steps!: QueryList<WizardStepComponent>;

    stepIndex!: number;
    stepIndexChange = new EventEmitter<number>();
    get canMoveBack(): boolean {
        return this.stepIndex > 0;
    }
    get canMoveNext(): boolean {
        return this.stepIndex < this.steps.length - 1;
    }

    currentStep!: WizardStepComponent;
    @Output() currentStepChange = new EventEmitter<WizardStepComponent>();
    @Output() next = new EventEmitter();
    @Output() back = new EventEmitter();

    @Output() completed = new EventEmitter();

    @Input()
    finishButtonText = 'Finish';

    constructor(private changeDetector: ChangeDetectorRef) {
    }

    ngOnInit(): void {
        console.log("wizard dialog init");

        if (this.steps != null && this.steps.length > 0)
            this.setStepIndex(0);
    }

    onShowing = () => {
        if (this.steps.length > 0)
            setTimeout(() => { this.setStepIndex(0) }, 0);
    }

    onShown = () => {
        this.visibleChange.emit(this.visible);
        this.shown.emit();
    }

    nextStep = () => {
        if (this.canMoveNext) {
            this.next.emit();
            if (this.currentStep.checkValid())
                this.setStepIndex(this.stepIndex + 1);
        }
    }

    prevStep = () => {
        if (this.canMoveBack) {
            this.back.emit();
            this.setStepIndex(this.stepIndex - 1);
        }
    }

    setStepIndex = (index: number) => {
        if (this.stepIndex === index)
            return;

        this.stepIndex = index;
        this.stepIndexChange.emit(this.stepIndex);
        this.updateCurrentStep();
    }

    private updateCurrentStep() {
        this.steps.forEach((step, i) => {
            var current = i === this.stepIndex;
            step.setCurrent(current);
            if (current) {
                this.currentStep = step;
                this.currentStepChange.emit(step);
            }
        });
    }

    complete = () => {
        var stepsArray = this.steps.toArray();

        var invalidIndex = stepsArray.findIndex(x => !x.checkValid());

        if (invalidIndex === -1) {
            this.completed.emit();
        } else {
            this.setStepIndex(invalidIndex);
        }
    }

    hide = () => {
        this.visible = false;
    }

    onHidden = () => {
        this.visibleChange.emit(false);

        this.hidden.emit();
    }
}
