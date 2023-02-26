import { Helper } from "../helpers/Helper";
import { TimeSpan } from "./TimeSpan";

export enum TimeIntervalType {
    Hour = 0,
    Minute = 1
}

export class DailyIntervals {
    private static weekDays = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    intervals: { [index: string]: Interval[]; } = {};
    offsetMinutes: number = 0;

    getAllIntervals(): { weekDay: number, intervals: Interval[] }[] {
        return DailyIntervals.weekDays.map((weekDay, i) => {
            return {
                weekDay: i,
                intervals: this.intervals[weekDay]
            }
        });
    }

    isEmpty(): boolean {
        return !this.getAllIntervals().some(x => x.intervals != null && x.intervals.length > 0);
    }

    static Copy(obj: any) {
        let copy = new DailyIntervals();

        copy.offsetMinutes = Helper.Coalesce(obj.offsetMinutes, obj.OffsetMinutes);

        const intervals = Helper.Coalesce(obj.intervals, obj.Intervals);
        if (intervals != null) {
            for (var weekDay of this.weekDays) {
                const array = Helper.Coalesce(intervals[weekDay], intervals[weekDay.toLowerCase()], []);
                copy.intervals[weekDay] = Array.from(array).map(x => Interval.Copy(x));
            }
        }

        return copy;
    }
}

export class Interval {
    public Begin: TimeSpan;
    public End: TimeSpan;

    constructor(begin: TimeSpan, end: TimeSpan) {
        this.Begin = begin || new TimeSpan();
        this.End = end || new TimeSpan(24);
    }

    static Copy(obj: any): Interval {
        if (obj == null) {
            return new Interval(new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 0));
        }

        const begin = TimeSpan.Copy(obj.Begin || obj.begin);
        const end = TimeSpan.Copy(obj.End || obj.end);
        const copy = new Interval(begin, end);
        return copy;
    }
}