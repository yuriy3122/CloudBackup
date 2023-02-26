
export class MonthlyScheduleParam {
    public DayOfMonth: number;
    public TimeOfDay: string;
    public MonthList: Array<number>;

    constructor() {
        this.DayOfMonth = 0;
        this.TimeOfDay = '';
        this.MonthList = [];
    }

    static Copy(obj: any): MonthlyScheduleParam {
        let copy = new MonthlyScheduleParam();

        copy.DayOfMonth = obj.DayOfMonth || obj.dayOfMonth;
        copy.TimeOfDay = obj.TimeOfDay || obj.timeOfDay;

        copy.MonthList = [];

        if (obj.MonthList != null && Array.isArray(obj.MonthList)) {
            for (var i = 0; i < obj.MonthList.length; i++)
                copy.MonthList.push(obj.MonthList[i]);
        }

        if (obj.monthList != null && Array.isArray(obj.monthList)) {
            for (var i = 0; i < obj.monthList.length; i++)
                copy.MonthList.push(obj.monthList[i]);
        }

        return copy;
    }
}