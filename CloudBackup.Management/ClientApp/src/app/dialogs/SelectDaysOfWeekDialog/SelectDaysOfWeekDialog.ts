//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, DoCheck, ViewChild, ChangeDetectorRef } from '@angular/core';
import { DxCheckBoxComponent, DxDateBoxComponent, DxTemplateModule, DxScrollViewComponent } from 'devextreme-angular';
import { DataGridSettings } from '../../classes';
//services
import { NotificationService } from '../../services/common/NotificationService';

@Component({
    selector: 'select-days-dialog',
    templateUrl: './SelectDaysOfWeekDialog.template.html'
})

@Injectable()
export class SelectDaysOfWeekDialog implements OnInit, DoCheck {
    public cancelButton: any;
    public days: Array<any> = [false, false, false, false, false, false, false];
    @Input()
    public selectedDays: Array<any> = [];
    @Output() selectedDaysChange = new EventEmitter();
    //popup
    public popup: any;
    @Input()
    public popupVisible: boolean = false;
    @Output() popupVisibleChange = new EventEmitter();
    public gridsettings = new DataGridSettings();
    constructor(public notificator: NotificationService, public changeDetector: ChangeDetectorRef) {
    }

    ngDoCheck() {
        this.popupVisibleChange.emit(this.popupVisible);
    }

    ngOnInit() {
        console.log('select days dialog ngInit');
    }

    onShow = () => {
        this.days = ([] as Array<number>).concat(this.selectedDays);
    }

    repaintPopup() {
        this.popup.repaint();
    }

    onHiding() {
        this.popupVisibleChange.emit(false);
    }

    save() {
        this.selectedDays = this.days;
        this.selectedDaysChange.emit(([] as Array<number>).concat(this.selectedDays));
    }

    hide() {
        this.popupVisible = false;
        this.changeDetector.detectChanges();
    }

    ok = () => {
        this.save();
        this.hide();
    }
}
