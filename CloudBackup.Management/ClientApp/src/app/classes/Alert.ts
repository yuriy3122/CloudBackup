export enum AlertType {
    Info,
    Warning,
    Error
}

export class Alert {
    Id: number;
    RowVersion: string;
    Date: Date;
    DateText: string;
    Type: AlertType;
    Message: string;
    Subject: string;
    IsNew: boolean;

    constructor(id?: number) {
        this.Id = id != null ? id : 0;
        this.Message = this.Subject = this.DateText = "";
    }

    static Copy(obj: any): Alert {
        let copy = new Alert(obj.Id || obj.id);
        copy.Date = obj.Date || obj.date;
        copy.DateText = obj.DateText || obj.dateText || "";
        copy.Type = obj.Type || obj.type;
        copy.Message = obj.Message || obj.message || "";
        copy.Subject = obj.Subject || obj.subject || "";

        return copy;
    }
}