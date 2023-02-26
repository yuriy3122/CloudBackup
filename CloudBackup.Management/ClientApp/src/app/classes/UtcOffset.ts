import { TimeSpan } from './TimeSpan';

export class UtcOffset {
    Offset: TimeSpan;
    Name: string;

    static Copy(obj: any): UtcOffset {
        const copy = new UtcOffset();
        if (obj == null)
            return copy;

        copy.Name = obj.Name || obj.name || "";

        const offset = obj.offset || obj.Offset;
        copy.Offset = TimeSpan.Copy(offset);

        return copy;
    }
}
