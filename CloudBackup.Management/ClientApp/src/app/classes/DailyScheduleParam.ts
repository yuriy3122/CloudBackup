
export class DailyScheduleParam {
    public Time: string;
    public Days: Array<number> = [];

    constructor(time: string, days: Array<number>) {
        this.Time = time;
        this.Days = ([] as Array<number>).concat(days);
    }

    static Copy(obj: any): DailyScheduleParam {
        let copy = new DailyScheduleParam(obj.Time || obj.time, obj.Days || obj.days);
        return copy;
    }
}