export class DelayedScheduleParam {
    public RunAtDateTime: string;

    constructor(time: string) {
        this.RunAtDateTime = time;
    }

    static Copy(obj: any): DelayedScheduleParam {
        let copy = new DelayedScheduleParam(obj.RunAtDateTime || obj.runAtDateTime);
        return copy;
    }
}