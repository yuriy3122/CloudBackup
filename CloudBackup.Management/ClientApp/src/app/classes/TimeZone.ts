import { TimeSpan } from './TimeSpan';

export class TimeZone {
    Id!: string;
    Name!: string;
    ShortName!: string;
    UtcOffset!: TimeSpan | null;

    static Copy(obj: any): TimeZone {
        const copy = new TimeZone();
        copy.Id = obj.Id || obj.id || "";
        copy.Name = obj.Name || obj.name || "";
        copy.ShortName = obj.ShortName || obj.shortName || obj.shortname || "";
        const utcOffset = obj.UtcOffset || obj.utcOffset || obj.utcoffset;
        copy.UtcOffset = utcOffset == null ? null : TimeSpan.Copy(utcOffset);
        return copy;
    }
}