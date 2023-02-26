import { Component, Injectable, Input, Output, OnInit, EventEmitter, ChangeDetectorRef } from "@angular/core";
import { DataGridSettings } from "../../classes";
import { ConfirmDialogContext } from './ConfirmDialogContext';

@Component({
    selector: 'confirm-dialog',
    templateUrl: './ConfirmDialog.template.html'
})
@Injectable()
export class ConfirmDialog implements OnInit {
    @Input()
    context: ConfirmDialogContext;
    public dialogResult!: boolean;
  public gridsettings = new DataGridSettings();
    // popup
    @Input()
    public visible: boolean = false;
    @Output()
    visibleChange = new EventEmitter<boolean>();

    constructor(private changeDetector: ChangeDetectorRef) {
    }

    ngOnInit(): void {
        console.log('confirm dialog: OnInit');
    }

    onShow = () =>{
        this.dialogResult = false;
        this.changeDetector.detectChanges();
        this.visibleChange.emit(this.visible);
    }

    hide = () => {
        this.visible = false;
        this.changeDetector.detectChanges();
    }

    onHiding = () => {
        this.visibleChange.emit(false);
        if (this.context != null && this.context.callback) {
            this.context.callback(this.dialogResult);
        }
    }

    setResult = (result: boolean) => {
        this.dialogResult = result;
        this.hide();
    }

    yes = () => {
        this.setResult(true);
    }
    
    no = () => {
        this.setResult(false);
    }
}
