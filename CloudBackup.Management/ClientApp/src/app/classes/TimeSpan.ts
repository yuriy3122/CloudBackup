import { Helper } from '../helpers/Helper';

export class TimeSpan {
    readonly TotalMilliseconds: number;

    constructor(hours: number = 0, minutes: number = 0, seconds: number = 0, milliseconds: number = 0) {
        this.TotalMilliseconds = milliseconds + seconds * 1000 + minutes * 60 * 1000 + hours * 60 * 60 * 1000;
    }

    get TotalSeconds(): number {
        return this.TotalMilliseconds / 1000;
    }

    get TotalMinutes(): number {
        return this.TotalSeconds / 60;
    }

    get TotalHours(): number {
        return this.TotalMinutes / 60;
    }

    get TotalDays(): number {
        return this.TotalHours / 24;
    }

    get Milliseconds(): number {
        return this.TotalMilliseconds % 1000;
    }

    get Seconds(): number {
        return Helper.Truncate(this.TotalSeconds) % 60;
    }

    get Minutes(): number {
        return Helper.Truncate(this.TotalMinutes) % 60;
    }

    get Hours(): number {
        return Helper.Truncate(this.TotalHours) % 24;
    }

    get Days(): number {
        return Helper.Truncate(this.TotalDays);
    }

    compareTo(other: TimeSpan): boolean {
        return this.TotalMilliseconds === other.TotalMilliseconds;
    }

    toString(): string {
        return this.toJSON();
    }

    toJSON(): string {
        let s = '';
        if (this.TotalMilliseconds < 0)
            s += '-';
        if (this.Days !== 0)
            s += Math.abs(this.Days) + '.';

        s += `${Helper.PadLeftWithZeros(Math.abs(this.Hours), 2)}:${Helper.PadLeftWithZeros(Math.abs(this.Minutes), 2)}:${Helper.PadLeftWithZeros(Math.abs(this.Seconds), 2)}`;

        if (this.Milliseconds !== 0)
            s += `.${Helper.PadLeftWithZeros(Math.abs(this.Milliseconds), 3)}`;

        return s;
    }

    static Copy(timeSpan: any): TimeSpan {
        let hours: number = 0;
        let minutes: number = 0;
        let seconds: number = 0;
        let milliseconds: number = 0;

        // if timeSpan is a string from .NET TimeSpan
        if (timeSpan != null && typeof (timeSpan) == 'string' && timeSpan.indexOf(':') > -1) {
            let parts = timeSpan.split(":");

            // sign checking
            let signChar = timeSpan[0];
            let sign = 1;
            if (signChar === '-')
                sign = -1;
            if (signChar === '-' || signChar === '+')
                parts[0] = parts[0].slice(1);


            if (parts.length > 0) {
                // days checking
                let days = 0;
                let dayEndIndex = parts[0].indexOf('.');
                if (dayEndIndex !== -1) {
                    days = parseInt(parts[0].slice(0, dayEndIndex));
                    parts[0] = parts[0].slice(dayEndIndex + 1);
                }

                hours = (parseInt(parts[0]) + days * 24) * sign;
            }
            if (parts.length > 1)
                minutes = parseInt(parts[1]) * sign;
            if (parts.length > 2) {
                // milliseconds checking
                let millisecondsStartIndex = parts[2].indexOf('.');
                if (millisecondsStartIndex !== -1) {
                    milliseconds = parseInt(parts[2].slice(millisecondsStartIndex + 1)) * sign;
                    parts[2] = parts[2].slice(0, millisecondsStartIndex - 1);
                }

                seconds = parseInt(parts[2]) * sign;
            }
        }


        if (timeSpan != null && typeof (timeSpan) == 'string' && timeSpan.indexOf('PT') > -1) {
            timeSpan = timeSpan.split("PT")[1].toLowerCase();
            var hourIndex = timeSpan.indexOf('h');
            if (hourIndex > -1) {
                hours = parseInt(timeSpan.slice(0, hourIndex));
                timeSpan = timeSpan.substring(hourIndex + 1);
            }

            var minuteIndex = timeSpan.indexOf('m');
            if (minuteIndex > -1) {
                minutes = parseInt(timeSpan.slice(0, minuteIndex));
                timeSpan = timeSpan.substring(minuteIndex + 1);
            }

            var secondIndex = timeSpan.indexOf('s');
            if (secondIndex > -1)
                seconds = parseInt(timeSpan.slice(0, secondIndex));
        }

        //if timespan is a object
        if (timeSpan != null && timeSpan instanceof TimeSpan) {// if timeSpan is same class 
            seconds = timeSpan.TotalSeconds;
        }
        else if (timeSpan != null && typeof (timeSpan) == 'object') {
            var obj = timeSpan as any;
            seconds = obj.Seconds || obj.seconds || 0;
            minutes = obj.Minutes || obj.minutes || 0;
            hours = obj.Hours || obj.hours || 0;
        }

        return new TimeSpan(hours, minutes, seconds);
    }
}
