
export class JobTag {
    Id: number;
    RowVersion: string;
    KeyName: string;
    Value: string;

    constructor() {
        this.Id = 0;
        this.KeyName = "";
        this.Value = "";
    }

    static Copy(obj: any): JobTag {
        let copy = new JobTag();

        copy.Id = obj.Id || obj.id;
        copy.KeyName = obj.KeyName || obj.keyName || "";
        copy.Value = obj.Value || obj.value || "";

        return copy;
    }
}