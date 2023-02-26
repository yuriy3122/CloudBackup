import { Guid } from '../classes/Guid';
import { HttpHeaders } from '@angular/common/http';


export class Helper {
    static AlphaNumericNotStartsWithNumberRegex = new RegExp(/^\D\w*$/, 'i');
    static PasswordRegex = new RegExp(/^[!-~]+$/);
    static EmailRegex = new RegExp(/^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/, 'i');
    static RdsIdentifierRegex = new RegExp(/^(?!.*--)[a-zA-Z](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?$/);

    static InArray(value: any, array: Array<any>, comparator?: string): number {
        comparator = comparator || "Id";
        if (array == undefined || array == null || !Array.isArray(array) || array.length == 0)
            return -1;

        for (var i = 0; i < array.length; ++i) {
            if (value[comparator] == null)
                return -1;
            if (typeof (value[comparator]) == 'number' && value[comparator] === array[i][comparator]) {
                return i;
            }
            if (typeof (value[comparator]) == 'string' && value[comparator].toLowerCase() === array[i][comparator].toLowerCase()) {
                return i;
            }
        }
        return -1;
    }

    static isInteger(value: number) {
        return !isNaN(value) && (value | 0) === value;
    }

    static setActiveMenu(route: any, siblings: any) {
        for (var i in siblings) {
            if (siblings[i].path == route.path)
                siblings[i].data.active = true;
            else
                siblings[i].data.active = false;
        }
    }

    static DistinctArray(array: Array<any>, comparator?: string): Array<any> {
        let newArray: Array<any> = [];
        comparator = comparator || "Id";
        if (array == undefined || array == null || !Array.isArray(array) || array.length == 0)
            return array;

        for (var i = 0; i < array.length; ++i) {
            if (Helper.InArray(array[i], newArray, comparator) < 0) {
                newArray.push(array[i]);
            }
        }
        return newArray;
    }

    static AllIndexesInArray(value: any, array: Array<any>, comparator?: string): Array<any> {
        let indexes: Array<any> = [];
        comparator = comparator || "Id";
        if (array == undefined || array == null || !Array.isArray(array) || array.length == 0)
            return indexes;

        for (var i = 0; i < array.length; ++i) {
            if (value[comparator] == null)
                continue;
            if (typeof (value[comparator]) == 'number' && value[comparator] === array[i][comparator]) {
                indexes.push(i);
            }
            if (typeof (value[comparator]) == 'string' && value[comparator].toLowerCase() === array[i][comparator].toLowerCase()) {
                indexes.push(i);
            }
        }
        return indexes;
    }

    static CheckRule(forCheck: any): string {
        var message = "";
        if (forCheck == null) {
            message += "Rule is null.";
        }
        else {
            if (forCheck.RuleName == null) {
                message += "Rule Name is empty.";
            }
            if (forCheck.VMId == null || forCheck.VMId == Guid.Empty.ToString()) {
                message += "Virtual Machine Id is empty";
            }
        }
        return message;
    }

    static DataSourceGroupBy(array: Array<any>, key: any) {
        var result: any[] = [];
        var gropped = Helper.GroupBy(array, key);
        for (var k in gropped) {
            result.push({ key: k, items: gropped[k] });
        }
        return result;
    }

    static GroupBy(array: Array<any>, key: any) {
        return array.reduce(function (rv, x) {
            (rv[x[key]] = rv[x[key]] || []).push(x);
            return rv;
        }, {});
    }

    static GetTimeFromTimeSpanStr(value: string): Date | null {
        if (value == null) return null;

        var date = new Date();

        var timePart = value.split(".").find(x => x.includes(':'));
        if (timePart) {
            var parts = timePart.split(":");
            date.setHours(Number(parts[0]));
            date.setMinutes(Number(parts[1]));
            date.setSeconds(Number(parts[2]));
        }
        return date;
    }

    //parse date as ISO 8601 "2017-07-14T16:41:29.1466613Z"
    static GetDateFromISODateStr(value: string): Date {
        var date = new Date();

        var parts = value.split("T");
        var datePart = parts[0];
        var dateParts = datePart.split("-");

        date.setFullYear(Number(dateParts[0]));
        date.setMonth(Number(dateParts[1]) - 1);
        date.setDate(Number(dateParts[2]));

        var timePart = parts[1];
        var timeZoneParts = timePart.split(".");
        var timeParts = timeZoneParts[0].split(":");
        date.setHours(Number(timeParts[0]));
        date.setMinutes(Number(timeParts[1]));
        date.setSeconds(Number(timeParts[2]));

        return date;
    }

    static GetDateTimeISOFormated(value: Date | null): string {
        if (value == null) return '';

        var year = value.getFullYear();
        var month = value.getMonth() + 1;
        var day = value.getDate();
        var hour = value.getHours();
        var minutes = value.getMinutes();
        var seconds = value.getSeconds();

        var date = year + "-" +
            (month < 10 ? "0" : "") + month + "-" +
            (day < 10 ? "0" : "") + day + "T" +
            (hour < 10 ? "0" : "") + hour + ":" +
            (minutes < 10 ? "0" : "") + minutes + ":" +
            (seconds < 10 ? "0" : "") + seconds;

        return date;
    }

    static GetTimeSpanISOFormated(value: Date): string {
        var hour = value.getHours();
        var minutes = value.getMinutes();
        var seconds = value.getSeconds();

        var date = (hour < 10 ? "0" : "") + hour + ":" +
            (minutes < 10 ? "0" : "") + minutes + ":" +
            (seconds < 10 ? "0" : "") + seconds;

        return date;
    }

    static PadLeftWithZeros(n: number, width: number): string {
        var n_ = Math.abs(n);
        var zeros = Math.max(0, width - Math.floor(n_).toString().length);
        var zeroString = Math.pow(10, zeros).toString().substr(1);
        if (n < 0) {
            zeroString = '-' + zeroString;
        }

        return zeroString + n_;
    }

    static Truncate(value: number): number {
        if (value < 0) {
            return Math.ceil(value);
        }

        return Math.floor(value);
    }

    static GetFileName(path: string): string {
        return path.replace(/^.*[\\\/]/, '');
    }

    static GetFileNameFromResponse(headers: HttpHeaders) {
        var contentDispositionHeader = headers.get('Content-Disposition');
        if (contentDispositionHeader == null)
            return null;

        return Helper.GetFileNameFromContentDispositionHeader(contentDispositionHeader);
    }

    static GetFileNameFromContentDispositionHeader(header: string) {
        var result = header.split(';')[1].trim().split('=')[1];
        return result.replace(/"/g, '');
    }

    static GetErrorMessage(e: any): string {
        if (e == null)
            return "";

        if (typeof e === 'string' || e instanceof String)
            return e as string;

        if (e.message != null)
            return e.message;

        if (e.responseJSON && e.responseJSON.message)
            return e.responseJSON.message;

        if (typeof e.json === 'function') {
            let data: any = null;
            try {
                data = e.json();
            } catch (e) { }
            if (data && data.message)
                return data.message;
        }

        return "";
    }

    static Coalesce<T>(...items: T[]): T | null {
        for (var item of items) {
            if (item != null)
                return item;
        }

        return null;
    }

    static BooleanFromString(s: string): boolean {
        if (s == null)
            return (s as any) as boolean;

        switch (s.toLowerCase()) {
            case "true":
                return true;
            case "false":
                return false;
            default:
                return false;
        }
    }
}