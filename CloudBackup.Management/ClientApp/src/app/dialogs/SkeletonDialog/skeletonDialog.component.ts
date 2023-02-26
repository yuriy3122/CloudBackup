import { Component, DoCheck, EventEmitter, Injectable, Input, OnChanges, OnInit, Output, SimpleChanges } from "@angular/core";
import { DataGridSettings } from "../../classes";


@Component({
    selector: 'skeleton-dialog',
    templateUrl: './SkeletonDialog.template.html'
})

@Injectable()
export class SkeletonDialog implements OnInit, DoCheck, OnChanges {
    visible = false;
    @Input()
    public popupVisible!: boolean;
    public gridsettings = new DataGridSettings();

    @Input()
    public value!: number;
    
    valueString!: string;

    @Output('textChange') emitter: EventEmitter<string> = new EventEmitter<string>();
    @Input('text') set currentVal(val: string) {
      this.valueString = val;
    }

    @Output() onHide = new EventEmitter();
    @Output() onComplete = new EventEmitter();

    title = "Some basic dialog";

    constructor() { }

    ngOnChanges(changes: SimpleChanges): void {
        console.log(changes);
    }

    ngDoCheck(): void {

    }

    ngOnInit(): void {

    }

    complete = () => {
        this.onComplete.emit(this.value);
        this.emitter.emit(this.valueString);
        this.close();
    }

    // lambda function to prevent losing "this" context from our component
    // to devextreme component
    close = () => {
        this.visible = false;
        this.onHiding();
    }

    onHiding = () => {
        this.onHide.emit(false);
    }

    onShowing = () => {

    }
}
