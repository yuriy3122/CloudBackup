import {TimeSpan} from "./TimeSpan";

export class Configuration {
    public ConfigurationStatus: string;
    public Email: string;
    public InstanceId: string;
    public Password: string;
    public UtcOffset: TimeSpan;
    public UserName: string;

    constructor() {
        this.ConfigurationStatus =
        this.Email =
        this.InstanceId =
        this.Password =
        this.UserName = "";
    }
}
