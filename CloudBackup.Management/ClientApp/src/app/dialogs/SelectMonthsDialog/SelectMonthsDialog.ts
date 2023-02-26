//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, DoCheck, ChangeDetectorRef } from '@angular/core';
import { DataGridSettings } from '../../classes';
//services
import { NotificationService } from '../../services/common/NotificationService';

@Component({
    selector: 'select-months-dialog',
    templateUrl: './SelectMonthsDialog.template.html'
})

@Injectable()
export class SelectMonthsDialog implements OnInit, DoCheck {
    public gridsettings = new DataGridSettings();
    public cancelButton: any;
    public months: Array<any> = [];
    @Input()
    public selectedMonths: Array<any> = [];
    @Output() selectedMonthsChange = new EventEmitter();
    //popup
    public popup: any;
    @Input()
    public popupVisible: boolean = false;
    @Output() popupVisibleChange = new EventEmitter();

    public toolbarItems = [{
        template: '<div class="button primary" id="okSelectMonthsButton"></div>',
        location: 'after',
        toolbar: 'bottom'
    }];

    constructor(public notificator: NotificationService, public changeDetector: ChangeDetectorRef) {
        this.months = Array(12).fill(false);
    }

    ngDoCheck() {
        this.popupVisibleChange.emit(this.popupVisible);
    }

    ngOnInit() {
        console.log('select months dialog ngInit');
    }

    onShow = () => {
        this.months = Array.from(this.selectedMonths);
    }

    repaintPopup() {
        this.popup.repaint();
    }

    onHiding() {
        this.popupVisibleChange.emit(false);
    }

    save() {
        this.selectedMonths = this.months;
        this.selectedMonthsChange.emit(Array.from(this.selectedMonths));
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
