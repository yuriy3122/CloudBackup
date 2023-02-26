import { Component, ElementRef, ViewChild, Input, Output, EventEmitter, ChangeDetectorRef, HostListener, SimpleChanges, AfterContentInit, AfterViewInit } from '@angular/core';
import { EnumValues } from "../../../classes/EnumValues";
import { DayOfWeek } from "../../../classes/Enums";
import { Helper } from "../../../helpers/Helper";
import { Point } from "../../../classes/Point";
import { TimeSpan } from "../../../classes/TimeSpan";
import { DailyIntervals, Interval } from "../../../classes/DailyIntervals";

class IntervalState {
    enabled = false;
    hover = false;
}

@Component({
    selector: 'daily-intervals-selector',
    templateUrl: './daily-intervals-selector.template.html'
})
export class DailyIntervalsSelectorComponent implements AfterContentInit, AfterViewInit {
    @Input()
    dailyIntervals!: DailyIntervals;
    @Output() dailyIntervalsChange = new EventEmitter<DailyIntervals>();

    @Input() emptyAsFull: boolean = false; // Treat no intervals as all enabled. For example, all permitted can be the same as no restrictions
    @Input() enabledText: string = "Enabled";
    @Input() disabledText: string = "Disabled";

    public internalDailyIntervals!: DailyIntervals; // Used to distinguish our changes from external changes.

    weekDayNames: string[] = ["All", ...EnumValues.getNames(DayOfWeek)];
    hourNames = Array.from(Array(24).keys()).map(hour => Helper.PadLeftWithZeros(hour % 12 || 12, 2));
    drawMode = true; // 1 - enable, 0 - disable
    offsetMinutes!: number;
    public min = 0;
    public max = 59;
    // canvas
    @ViewChild('canvas')
    public canvas!: ElementRef;
    public context!: CanvasRenderingContext2D;
    public animation: any;

    // drawing
    public intervalStates!: IntervalState[][];
    public selectionStartPosition!: Point | null;
    public selectionCurrentPosition!: Point | null;
    public verticalMargin = 5;
    public relativeHorizontalMargin = 0.2;
    get initialized() { return this.context != null; }
    get blockHeight() {
        return (this.context.canvas.height - (this.intervalStates.length - 1) * this.verticalMargin) /
            this.intervalStates.length;
    }
    get blockWidth() { return this.context.canvas.width / (24 + 23 * this.relativeHorizontalMargin); }
    get horizontalMargin() { return this.blockWidth * this.relativeHorizontalMargin }

    //shouldn't we move this to some Color-Constants file?
    disabledColor = "#e2e2e2";
    disabledHoverColor = "#f2f2f2";
    enabledColor = "#5282FF";
    enabledHoverColor = "var(--accent-color-hover)";

    constructor(public changeDetector: ChangeDetectorRef) {

    }

    ngAfterContentInit() {
        this.init();
    }

    ngAfterViewInit() {
        this.context = (<HTMLCanvasElement>this.canvas.nativeElement).getContext('2d') || new CanvasRenderingContext2D();

        this.draw();

        this.changeDetector.detectChanges();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes.hasOwnProperty("dailyIntervals") && changes.dailyIntervals.currentValue !== this.internalDailyIntervals) {
            if (this.initialized) {
                this.init();
                this.draw();
            }
        }
    }

    public init() {
        const dailyIntervals = this.dailyIntervals || new DailyIntervals();
        this.offsetMinutes = dailyIntervals.offsetMinutes;

        // fill states
        const showFull = this.emptyAsFull && dailyIntervals.isEmpty();

        const intervalsMap = new Map<number, Interval[]>();
        for (let dailyInterval of dailyIntervals.getAllIntervals()) {
            intervalsMap.set(dailyInterval.weekDay, dailyInterval.intervals);
        }

        this.intervalStates = new Array();
        for (let weekDay = 0; weekDay < 7; weekDay++) {
            const intervals = intervalsMap.get(weekDay) || [];

            const row = new Array<IntervalState>();
            for (var hour = 0; hour < 24; hour++) {
                const state = new IntervalState();
                // If no intervals are set (isEmpty === true), show all as enabled
                state.enabled = showFull || intervals.some(x => x.Begin.TotalHours <= hour && hour < x.End.TotalHours);
                row.push(state);
            }
            this.intervalStates.push(row);
        }

        this.changeDetector.detectChanges();
    }

    setWeekDay(weekDayIndex: number) {
        if (weekDayIndex < 0 || weekDayIndex > this.intervalStates.length)
            return;

        const weekDays = weekDayIndex === 0 ? Array.from(this.weekDayNames.keys()).slice(1) : [weekDayIndex];

        for (let weekDay of weekDays) {
            for (let state of this.intervalStates[weekDay - 1]) {
                state.enabled = this.drawMode;
            }
        }

        this.draw();
        this.onStateChanged();

        this.changeDetector.detectChanges();
    }

    setHour(hour: number) {
        if (hour < 0 || hour > 23)
            return;

        for (let intervalStates of this.intervalStates) {
            const state = intervalStates[hour];
            if (state != null)
                state.enabled = this.drawMode;
        }

        this.draw();
        this.onStateChanged();

        this.changeDetector.detectChanges();
    }

    public getDailyIntervals(): DailyIntervals {
        const dailyIntervals = new DailyIntervals();

        dailyIntervals.offsetMinutes = this.offsetMinutes;

        if (this.emptyAsFull) {
            const isFull = this.intervalStates.every(intervals => intervals.every(state => state.enabled));
            if (isFull) // All permitted equals to no intervals
                return dailyIntervals;
        }

        for (let weekDay = 0; weekDay < 7; weekDay++) {
            const weekDayName = this.weekDayNames[weekDay + 1];
            const intervals = new Array<Interval>();
            let currentInterval: Interval | null = null;

            for (let hour = 0; hour < 24; hour++) {
                const state = this.intervalStates[weekDay][hour];
                const timeStart = new TimeSpan(hour);
                const timeEnd = new TimeSpan(hour + 1);

                if (state.enabled) {
                    if (currentInterval != null) {
                        currentInterval.End = timeEnd;
                    } else {
                        currentInterval = new Interval(timeStart, timeEnd);
                    }
                }

                if (currentInterval != null && (!state.enabled || hour === 23)) {
                    intervals.push(currentInterval);
                    currentInterval = null;
                }
            }

            if (intervals.length > 0)
                dailyIntervals.intervals[weekDayName] = intervals;
        }

        return dailyIntervals;
    }

    onMouseDown(e: any) {
        this.selectionStartPosition = this.selectionCurrentPosition = new Point(e.offsetX, e.offsetY);

        requestAnimationFrame(() => {
            this.draw();
        });

        e.stopPropagation();
        e.preventDefault();
    }

    @HostListener("document:mousemove", ["$event"])
    onMouseMove(e: any) {
        if (!this.initialized)
            return;

        const canvasRect = this.canvas.nativeElement.getBoundingClientRect();
        const inside =
            canvasRect.left <= e.clientX && e.clientX <= canvasRect.right &&
            canvasRect.top <= e.clientY && e.clientY <= canvasRect.bottom;

        if (inside || this.selectionStartPosition) {
            this.selectionCurrentPosition = new Point(e.clientX - canvasRect.left, e.clientY - canvasRect.top);

            if (!this.animation) {
                this.animation = requestAnimationFrame(() => {
                    this.updateHoverStates();
                    this.draw();
                    this.animation = null;
                });
            }

            e.stopPropagation();
            e.preventDefault();
        }
    }

    @HostListener("document:mouseup", ["$event"])
    onMouseUp(e: any) {
        if (!this.initialized || !this.selectionStartPosition)
            return;

        const canvasRect = this.canvas.nativeElement.getBoundingClientRect();
        this.selectionCurrentPosition = new Point(e.clientX - canvasRect.left, e.clientY - canvasRect.top);
        requestAnimationFrame(() => {
            this.updateHoverStates();
            this.applyHoverStates();

            this.draw();

            this.selectionStartPosition = this.selectionCurrentPosition = null;

            this.onStateChanged();
        });
    }

    onMouseLeave(e: any) {
        if (!this.selectionStartPosition) {
            // reset hover if no selection
            this.selectionCurrentPosition = null;
            this.resetHoverStates();
            this.draw();
        }
    }

    public onStateChanged() {
        this.dailyIntervals = this.getDailyIntervals();
        this.internalDailyIntervals = this.dailyIntervals;
        this.dailyIntervalsChange.emit(this.dailyIntervals);
    }

    public updateHoverStates() {
        const startPosition = this.selectionStartPosition || this.selectionCurrentPosition; // for mouse hover

        if (!startPosition || !this.selectionCurrentPosition)
            return;

        const selectionStart = new Point(
            Math.min(startPosition.x, this.selectionCurrentPosition.x),
            Math.min(startPosition.y, this.selectionCurrentPosition.y));
        const selectionEnd = new Point(
            Math.max(startPosition.x, this.selectionCurrentPosition.x),
            Math.max(startPosition.y, this.selectionCurrentPosition.y));

        for (let weekDay = 0; weekDay < 7; weekDay++) {
            for (let hour = 0; hour < 24; hour++) {
                const blockStart = this.getBlockPosition(weekDay, hour);
                const blockEnd = new Point(blockStart.x + this.blockWidth, blockStart.y + this.blockHeight);
                const isHovered =
                    blockStart.x <= selectionEnd.x && blockEnd.x >= selectionStart.x &&
                    blockStart.y <= selectionEnd.y && blockEnd.y >= selectionStart.y;

                const currentState = this.intervalStates[weekDay][hour];
                currentState.hover = isHovered;
            }
        }
    }

    public resetHoverStates() {
        for (let weekDay = 0; weekDay < 7; weekDay++) {
            for (let hour = 0; hour < 24; hour++) {
                const state = this.intervalStates[weekDay][hour];
                state.hover = false;
            }
        }
    }

    public applyHoverStates() {
        for (let weekDay = 0; weekDay < 7; weekDay++) {
            for (let hour = 0; hour < 24; hour++) {
                const currentState = this.intervalStates[weekDay][hour];
                if (currentState.hover) {
                    currentState.enabled = this.drawMode;
                    currentState.hover = false;
                }
            }
        }
    }

    public draw() {
        this.context.clearRect(0, 0, this.context.canvas.width, this.context.canvas.height);
        const blockWidth = this.blockWidth;
        const blockHeight = this.blockHeight;

        for (let weekDay = 0; weekDay < 7; weekDay++) {
            for (let hour = 0; hour < 24; hour++) {
                const state = this.intervalStates[weekDay][hour];
                this.context.fillStyle = this.getBlockColor(state);
                const blockPosition = this.getBlockPosition(weekDay, hour);

                this.context.fillRect(blockPosition.x, blockPosition.y, blockWidth, blockHeight);
            }
        }
    }

    public getBlockPosition(weekDay: number, hour: number): Point {
        return new Point(
            hour * (this.blockWidth + this.horizontalMargin),
            weekDay * (this.blockHeight + this.verticalMargin));
    }

    public getBlockColor(state: IntervalState) {
        return state.hover
            ? (this.drawMode ? this.enabledHoverColor : this.disabledHoverColor)
            : (state.enabled ? this.enabledColor : this.disabledColor);
    }
}
