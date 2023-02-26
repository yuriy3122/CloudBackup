import { Guid } from './Guid';

export enum MessageType {
    Success = 1,
    Warning,
    Error,
    Info
}

export class Message {
    public Id: string;
    public Title: string;
    public Description: string;
    public Message: string;
    public Type: MessageType;
    public Date: Date;

    constructor(message: string, title?: string, type?: MessageType, descr?: string) {
        this.Id = Guid.NewGuid().ToString();
        this.Message = message || "";
        this.Type = type || MessageType.Error;
        this.Title = title || "Error";
        this.Description = descr || "";
        this.Date = new Date();
    }

    static Success(message: string, title?: string, descr?: string) {
        return new Message(message, title, MessageType.Success, descr);
    }

    static Warning(message: string, title?: string, descr?: string) {
        return new Message(message, title, MessageType.Warning, descr);
    }

    static Error(message: string, title?: string, descr?: string) {
        return new Message(message, title, MessageType.Error, descr);
    }

    static Info(message: string, title?: string, descr?: string) {
        return new Message(message, title, MessageType.Info, descr);
    }
}

export class MessageList {
    public Messages: Array<Message>;
    public SuccsessCount: number;
    public FailCount: number;

    constructor() {
        this.Messages = new Array<Message>();
        this.SuccsessCount = this.FailCount = 0;
    }

    get length(): number {
        return this.Messages.length;
    }

    Add(message: Message) {
        this.Messages.push(message);
        this._recalc();
    }

    Remove(id: string) {
        var index = -1;
        for (var i = 0; i < this.Messages.length; i++) {
            if (this.Messages[i].Id == id) {
                index = i;
                break;
            }
        }

        if (index > -1) {
            var deleted = this.Messages.splice(index, 1);
            this._recalc();
        }
    }

    Clear() {
        this.Messages.length = 0;
        this.Messages = new Array<Message>();
        this.SuccsessCount = this.FailCount = 0;
    }

    private _recalc() {
        this.SuccsessCount = this.FailCount = 0;
        for (var i = 0; i < this.Messages.length; i++) {
            if (this.Messages[i].Type == MessageType.Success)
                this.SuccsessCount++;
            else if (this.Messages[i].Type == MessageType.Error)
                this.FailCount++;
        }
    }
}