import { TimeIntervalType, DailyIntervals } from "./DailyIntervals";

export class IntervalScheduleParam {
    public TimeIntervalType: TimeIntervalType;
    public TimeIntervalValue: number;
    public DailyIntervals: DailyIntervals;

    constructor(timeIntervalType: TimeIntervalType, timeIntervalValue: number) {
        this.TimeIntervalType = timeIntervalType;
        this.TimeIntervalValue = timeIntervalValue;

        this.DailyIntervals = new DailyIntervals();
    }

    static Copy(obj: any): IntervalScheduleParam {
        let copy = new IntervalScheduleParam(obj.timeIntervalType || obj.TimeIntervalType, obj.timeIntervalValue || obj.TimeIntervalValue);
        copy.DailyIntervals = DailyIntervals.Copy(obj.DailyIntervals || obj.dailyIntervals);

        return copy;
    }
}

