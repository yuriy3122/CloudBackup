import { Component, Input, SimpleChanges, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { Subject } from "rxjs";
import { Schedule } from "../../../classes/Schedule";
import { StartupType, OccurType } from "../../../classes/Enums";
import { IntervalScheduleParam } from "../../../classes/IntervalScheduleParam";
import { Helper } from "../../../helpers/Helper";
import { DailyScheduleParam } from "../../../classes/DailyScheduleParam";
import { MonthlyScheduleParam } from "../../../classes/MonthlyScheduleParam";
import { TimeIntervalType, DailyIntervals } from "../../../classes/DailyIntervals";
import { debounceTime, map } from 'rxjs/operators';

enum ScheduleDayGroupOption {
    EveryDay = 0,
    WeekDays = 1,
    TheseDays = 2
}
enum ScheduleDayOfMonthGroupOption {
    First = 1,
    Second = 2,
    Third = 3,
    Fourth = 4,
    Last = -1,
    ThisDay = 0
}

@Component({
    selector: 'job-schedule-editor',
    templateUrl: './job-schedule-editor.template.html',
    host: { 'style': 'display: block;' }
})
export class JobScheduleEditorComponent {
    // to reference from template
    OccurType = OccurType;
    ScheduleDayGroupOption = ScheduleDayGroupOption;
    ScheduleDayOfMonthGroupOption = ScheduleDayOfMonthGroupOption;

    @Input()
    disabled!: boolean;

    // Validation
    @Input()
    showValidation = true; // display validation in UI

    get isValid(): boolean {
        return this.isScheduleNameValid &&
            this.isDailyRunTimeValid &&
            (this.timeIntervalError == null || this.timeIntervalError == "")&&
            this.isDayOfMonthTimeValid &&
            this.isSelectedDaysValid &&
            this.isSelectedMonthsValid;
    }
    @Output()
    isValidChange = new EventEmitter<boolean>();

    // Schedule parameters
    @Input()
    schedule!: Schedule;
    @Output()
    scheduleChange = new EventEmitter<Schedule>();
    @Input()
    scheduleChangeExpectsValidate: boolean = true;

    private paramsChangeSubject = new Subject();
    private internalSchedule!: Schedule | undefined; // schedule objects that represents current params. Used to distinguish our changes from external changes.

    scheduleName!: string;
    isScheduleNameValid!: boolean;

    occurType!: OccurType;

    // Recurring periodically
    periods = [
        { text: "Hours", id: 0 },
        { text: "Minutes", id: 1 }
    ];
    timeIntervalType!: TimeIntervalType;
    timeIntervalValue!: number;
    selectDailyIntervalsPopupVisible = false;
    timeIntervalDays!: DailyIntervals;
    timeIntervalError!: string;

    // Recurring daily
    dailyRunTime: any = new Date();
    isDailyRunTimeValid!: boolean;

    dayGroupOptions = [
        { text: "Everyday", value: ScheduleDayGroupOption.EveryDay },
        { text: "On week-days", value: ScheduleDayGroupOption.WeekDays },
        { text: "On these days", value: ScheduleDayGroupOption.TheseDays }
    ];
    dayGroupOption = ScheduleDayGroupOption.EveryDay;

    // Daily - "These days"
    selectDaysOfWeekPopupVisible!: boolean;
    selectedDays!: Array<boolean>;
    isSelectedDaysValid!: boolean;

    // Recurring monthly
    dayOfMonthTime!: any;
    isDayOfMonthTimeValid!: boolean;

    dayOfMonthGroupOptions = [
        { text: "First", value: ScheduleDayOfMonthGroupOption.First },
        { text: "Second", value: ScheduleDayOfMonthGroupOption.Second },
        { text: "Third", value: ScheduleDayOfMonthGroupOption.Third },
        { text: "Fourth", value: ScheduleDayOfMonthGroupOption.Fourth },
        { text: "Last", value: ScheduleDayOfMonthGroupOption.Last },
        { text: "This day", value: ScheduleDayOfMonthGroupOption.ThisDay }
    ];
    dayOfMonthGroupOption = ScheduleDayOfMonthGroupOption.ThisDay;

    // Monthly - "This day"
    dayNumbers!: Array<any>;
    dayOfMonthOption!: number;

    selectMonthsPopupVisible!: boolean;
    selectedMonths!: Array<boolean>;
    isSelectedMonthsValid!: boolean;

    constructor(private changeDetector: ChangeDetectorRef) {
        this.resetParams();

        this.paramsChangeSubject.pipe(
            debounceTime(25)
        ).subscribe(() => this.onScheduleChanged());
    }

    ngOnInit() {
        this.dayNumbers = Array();
        for (var i = 1; i < 32; i++)
            this.dayNumbers.push({ text: i, id: i });
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes.hasOwnProperty("schedule") && this.schedule !== this.internalSchedule) {
            if (this.schedule != null)
                this.setScheduleParams(this.schedule);
            else
                this.reset();
        }
    }

    reset() {
        this.resetParams();
        this.changeDetector.detectChanges();
    }

    showDailyIntervalsPopup() {
        this.selectDailyIntervalsPopupVisible = true;
    }

    showDaysPopup() {
        this.selectDaysOfWeekPopupVisible = true;
    }

    showMonthsPopup() {
        this.selectMonthsPopupVisible = true;
    }

    setOccurType(type: OccurType) {
        this.occurType = type;
        this.onParamsChanged();
    }

    onParamsChanged() {
        this.paramsChangeSubject.next();
    }

    private onScheduleChanged() {
        this.validateScheduleParams();

        if (!this.scheduleChangeExpectsValidate || this.isValid) {
            this.internalSchedule = this.getSelectedSchedule();
            this.schedule = this.internalSchedule;
            this.scheduleChange.emit(this.schedule);
        }
    }

    setScheduleParams(schedule: Schedule) {
        this.reset();

        this.scheduleName = schedule.Name;
        this.occurType = schedule.OccurType;
        var param = schedule.Params ? JSON.parse(schedule.Params) : null;

        switch (this.occurType) {
            case OccurType.Daily:
                const dailyParam = param != null ? DailyScheduleParam.Copy(param) : null;
                if (dailyParam) {
                    this.selectedDays = Array(7).fill(false);
                    for (let day of dailyParam.Days) {
                        this.selectedDays[day] = true;
                    }
                    this.dailyRunTime = Helper.GetTimeFromTimeSpanStr(dailyParam.Time) || new Date(0, 0, 0);
                }
                this.setDayGroupOption();
                break;
            case OccurType.Periodically:
                const intervalParam = param != null ? IntervalScheduleParam.Copy(param) : null;
                if (intervalParam) {
                    this.timeIntervalType = intervalParam.TimeIntervalType;
                    this.timeIntervalValue = intervalParam.TimeIntervalValue;
                    this.timeIntervalDays = intervalParam.DailyIntervals || new DailyIntervals();
                }
                break;
            case OccurType.Monthly:
                const monthlyParam = param != null ? MonthlyScheduleParam.Copy(param) : null;
                if (monthlyParam) {
                    this.dayOfMonthTime = Helper.GetTimeFromTimeSpanStr(monthlyParam.TimeOfDay) || new Date(0, 0, 0);
                    this.dayOfMonthOption = monthlyParam.DayOfMonth;
                    this.selectedMonths = Array(12).fill(false);
                    for (let month of monthlyParam.MonthList) {
                        this.selectedMonths[month - 1] = true;
                    }
                }
                this.setMonthScheduleOptions();
                break;
        }

        this.validateScheduleParams();
    }

    getSelectedSchedule(): Schedule {
        let schedule: Schedule;

        if (this.schedule == null) {
            schedule = new Schedule();
        } else {
            schedule = Schedule.Copy(this.schedule);
        }


        schedule.Name = this.scheduleName;
        schedule.StartupType = StartupType.Recurring;
        schedule.OccurType = this.occurType;

        if (this.occurType === OccurType.Daily) {
            var days: Array<number> = [];

            if (this.dayGroupOption === ScheduleDayGroupOption.EveryDay) {
                days = [0, 1, 2, 3, 4, 5, 6];
            }
            else if (this.dayGroupOption === ScheduleDayGroupOption.WeekDays) {
                days = [1, 2, 3, 4, 5];
            }
            else if (this.dayGroupOption === ScheduleDayGroupOption.TheseDays) {
                for (let i = 0; i < this.selectedDays.length; i++) {
                    if (this.selectedDays[i]) {
                        days.push(i);
                    }
                }
            }

            var time = Helper.GetTimeSpanISOFormated(this.dailyRunTime ? this.dailyRunTime : new Date());
            var dailyParam = new DailyScheduleParam(time, days);
            schedule.Params = JSON.stringify(dailyParam);
        }
        else if (this.occurType === OccurType.Periodically) {
            var param = new IntervalScheduleParam(this.timeIntervalType, this.timeIntervalValue);
            param.DailyIntervals = this.timeIntervalDays;

            schedule.Params = JSON.stringify(param);
        }
        else if (this.occurType === OccurType.Monthly) {
            var monthlyParam = new MonthlyScheduleParam();
            monthlyParam.TimeOfDay = Helper.GetTimeSpanISOFormated(this.dayOfMonthTime ? this.dayOfMonthTime : new Date());

            switch (this.dayOfMonthGroupOption) {
                case ScheduleDayOfMonthGroupOption.Last:
                    monthlyParam.DayOfMonth = -1;
                    break;
                case ScheduleDayOfMonthGroupOption.ThisDay:
                    monthlyParam.DayOfMonth = this.dayOfMonthOption;
                    break;
                default:
                    monthlyParam.DayOfMonth = this.dayOfMonthGroupOption;
            }

            var months: Array<number> = [];
            for (let i = 0; i < this.selectedMonths.length; i++) {
                if (this.selectedMonths[i]) {
                    months.push(i + 1);
                }
            }

            monthlyParam.MonthList = months;
            schedule.Params = JSON.stringify(monthlyParam);
        }

        return schedule;
    }

    validateScheduleParams() {
        this.isDailyRunTimeValid = true;
        this.timeIntervalError = '';
        this.isDayOfMonthTimeValid = true;
        this.isSelectedDaysValid = true;
        this.isSelectedMonthsValid = true;

        this.isScheduleNameValid = this.scheduleName != null && this.scheduleName !== "";

        if (this.occurType === OccurType.Periodically && !Helper.isInteger(this.timeIntervalValue)) {
            this.timeIntervalError = "Interval must be an integer.";
        }
        if (this.occurType === OccurType.Periodically && this.timeIntervalValue === 0) {
            this.timeIntervalError = "Interval must be greater than zero.";
        }
        if (this.occurType === OccurType.Daily && this.dailyRunTime == null) {
            this.isDailyRunTimeValid = false;
        }
        if (this.occurType === OccurType.Monthly && this.dayOfMonthTime == null) {
            this.isDayOfMonthTimeValid = false;
        }
        if (this.occurType === OccurType.Daily && this.dayGroupOption === ScheduleDayGroupOption.TheseDays) {
            const days = Array(7).fill(false);

            if (JSON.stringify(days) == JSON.stringify(this.selectedDays)) {
                this.isSelectedDaysValid = false;
            }
        }
        if (this.occurType === OccurType.Monthly) {
            let months = Array(12).fill(false);
            if (JSON.stringify(months) == JSON.stringify(this.selectedMonths)) {
                this.isSelectedMonthsValid = false;
            }
        }

        this.isValidChange.emit(this.isValid);
    }

    private setDayGroupOption() {
        let everyday = Array(7).fill(true);
        let weekdays = [false, true, true, true, true, true, false];

        if (JSON.stringify(everyday) === JSON.stringify(this.selectedDays)) {
            this.dayGroupOption = ScheduleDayGroupOption.EveryDay;
        }
        else {
            if (JSON.stringify(weekdays) === JSON.stringify(this.selectedDays)) {
                this.dayGroupOption = ScheduleDayGroupOption.WeekDays;
            }
            else {
                this.dayGroupOption = ScheduleDayGroupOption.TheseDays;
            }
        }
    }

    private setMonthScheduleOptions() {
        if (this.dayOfMonthOption === -1) {
            this.dayOfMonthGroupOption = ScheduleDayOfMonthGroupOption.Last;
            this.dayOfMonthOption = 1;
        } else if (this.dayOfMonthOption <= 4)
            this.dayOfMonthGroupOption = this.dayOfMonthOption;
        else
            this.dayOfMonthGroupOption = ScheduleDayOfMonthGroupOption.ThisDay;
    }

    private resetParams() {
        this.internalSchedule = undefined;

        this.scheduleName = "";
        this.occurType = OccurType.Daily;
        this.timeIntervalType = TimeIntervalType.Hour;
        this.timeIntervalValue = 1;
        this.timeIntervalDays = new DailyIntervals();
        this.dailyRunTime = new Date(0, 0, 0);
        this.dayGroupOption = ScheduleDayGroupOption.EveryDay;
        this.selectedDays = Array(7).fill(true);

        this.dayOfMonthTime = new Date(0, 0, 0);
        this.dayOfMonthGroupOption = ScheduleDayOfMonthGroupOption.ThisDay;
        this.dayOfMonthOption = 1;
        this.selectedMonths = Array(12).fill(true);

        this.validateScheduleParams();
    }
}