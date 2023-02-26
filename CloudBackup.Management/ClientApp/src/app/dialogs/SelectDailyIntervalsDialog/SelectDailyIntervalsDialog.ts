//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, DoCheck, ViewChild, ChangeDetectorRef, AfterViewInit, ElementRef, ChangeDetectionStrategy, HostListener } from '@angular/core';
//services
import { NotificationService } from '../../services/common/NotificationService';

import { DailyIntervals } from "../../classes/DailyIntervals";

@Component({
    selector: 'select-daily-intervals-dialog',
    
    templateUrl: './SelectDailyIntervalsDialog.template.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SelectDailyIntervalsDialog implements OnInit {
    @Input()
    dailyIntervals!: DailyIntervals;
    @Output() dailyIntervalsChange = new EventEmitter<DailyIntervals>();
    //popup
    @Input()
    public popupVisible!: boolean;
    @Output() popupVisibleChange = new EventEmitter();

    public dailyIntervalsToEdit!: DailyIntervals;

    constructor(
        public notificator: NotificationService,
        public changeDetector: ChangeDetectorRef) {
    }

    ngOnInit() {
        console.log('select daily intervals dialog ngInit');
    }

    onShow() {
        this.dailyIntervalsToEdit = DailyIntervals.Copy(this.dailyIntervals);
        this.changeDetector.detectChanges();
    }

    save() {
        this.dailyIntervals = this.dailyIntervalsToEdit;
        this.dailyIntervalsChange.emit(this.dailyIntervals);

        this.hide();

        this.changeDetector.detectChanges();
    }

    hide() {
        this.popupVisible = false;
        this.changeDetector.detectChanges();
    }

    onHiding() {
        this.popupVisibleChange.emit(false);
    }
}