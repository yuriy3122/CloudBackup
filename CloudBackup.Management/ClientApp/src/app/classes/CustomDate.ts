export class CustomDate {
    constructor(public date?: Date) {
        if (date == null)
            date = new Date();
        this._date = date;
    }
    public Types = ['milliseconds', 'seconds', 'minutes', 'hours', 'days', 'weeks', 'months', 'years'];
    private _date: Date;
    public static Now = new CustomDate(new Date());

    public toString(): string {
        return this._date.toString();
    }

    getDate() {
        return this._date;
    }

    firstDayOfMonth(): CustomDate {
        this._date.setDate(1);
        return new CustomDate(this._date);
    }

    add(num: number, type: string): CustomDate {
        let index = this.Types.indexOf(type);
        if (index > -1) {
            let old = 0;
            switch (index) {
                case 0: old = this._date.getMilliseconds(); old += num; this._date.setMilliseconds(old); break;
                case 1: old = this._date.getSeconds(); old += num; this._date.setSeconds(old); break;
                case 2: old = this._date.getMinutes(); old += num; this._date.setMinutes(old); break;
                case 3: old = this._date.getHours(); old += num; this._date.setHours(old); break;
                case 4: old = this._date.getDate(); old += num; this._date.setDate(old); break;
                case 5: old = this._date.getDate(); old += num * 7; this._date.setDate(old); break;
                case 6: old = this._date.getMonth(); old += num; this._date.setMonth(old); break;
                case 7: old = this._date.getFullYear(); old += num; this._date.setFullYear(old); break;
            }
        }
        return new CustomDate(this._date);
    }

    toJSON() {
        return '\/Date(' + this._date.getTime() + ')\/';
    }
}