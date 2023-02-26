export class Guid {
    constructor(public guid: string) {
        this._guid = guid;
    }

    static generate(num: number): Guid {
        var result: string;
        var i: string;
        var j: number;

        result = "";
        for (j = 0; j < 32; j++) {
            if (j == 8 || j == 12 || j == 16 || j == 20)
                result = result + '-';
            i = num.toString(16).toUpperCase();
            result = result + i;
        }
        return new Guid(result);
    }

    private _guid: string;
    public static Empty = Guid.generate(0);//"00000000-0000-0000-0000-000000000000";
    public static FFFF = Guid.generate(15);//FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF

    public ToString(): string {
        return this._guid;
    }

    toJSON() {
        return this._guid;
    }

    // Static member
    static NewGuid(): Guid {
        var result: string;
        var i: string;
        var j: number;

        result = "";
        for (j = 0; j < 32; j++) {
            if (j == 8 || j == 12 || j == 16 || j == 20)
                result = result + '-';
            i = Math.floor(Math.random() * 16).toString(16).toUpperCase();
            result = result + i;
        }
        return new Guid(result);
    }
}