import { Tenant } from './Tenant';
import { DeliveryMethod } from './Enums';

export class NotificationDeliveryConfig {
    public Id: number; // hidden field
    public RowVersion: string;
    public Name: string;
    public TenantId: number;
    public TenantName?: string;
    public DeliveryMethod: DeliveryMethod;
    public SenderEmail: string;
    public EmailSmtpServer: string;
    public EmailSmtpPort: number;
    public EmailSmtpUserName: string;
    public EmailSmtpUserPassword: string;

    constructor(id?: number) {
        this.Id = id || 0;
        this.DeliveryMethod = 0;
        this.EmailSmtpPort = 0;
        this.Name = this.SenderEmail = this.RowVersion =
        this.EmailSmtpServer = this.EmailSmtpUserName =
        this.EmailSmtpUserPassword = "";
    }

    public get isNameValid(): boolean {
        return this.Name != null && this.Name.length > 0;
    }

    static Copy(obj: any): NotificationDeliveryConfig {
        if (obj == null)
            return new NotificationDeliveryConfig(0);

        let copy = new NotificationDeliveryConfig(obj.Id || obj.id);
        copy.RowVersion = obj.RowVersion || obj.rowVersion || "";
        copy.Name = obj.Name || obj.name || "";

        copy.TenantId = obj.TenantId || obj.id || 0;
        copy.TenantName = obj.TenantName || obj.tenantName || "";
        copy.DeliveryMethod = obj.DeliveryMethod || obj.deliveryMethod;
        copy.SenderEmail = obj.SenderEmail || obj.senderEmail;
        copy.EmailSmtpServer = obj.EmailSmtpServer || obj.emailSmtpServer;
        copy.EmailSmtpPort = obj.EmailSmtpPort || obj.emailSmtpPort;
        copy.EmailSmtpUserName = obj.EmailSmtpUserName || obj.emailSmtpUserName;
        copy.EmailSmtpUserPassword = obj.EmailSmtpUserPassword || obj.emailSmtpUserPassword;

        return copy;
    }
}
