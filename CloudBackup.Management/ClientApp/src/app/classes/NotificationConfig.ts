import { Helper } from '../helpers/Helper';
import { NotificationType } from './Enums';
import { NotificationDeliveryConfig } from './NotificationDeliveryConfig';

export class NotificationConfig {
    public Id: number; // hidden field
    public RowVersion: string;
    public Name: string;
    public Email: string;
    public Type: NotificationType;
    public TenantId: number;
    public TenantName?: string;
    public IncludeTenants: boolean | false;
    public DeliveryConfig!: NotificationDeliveryConfig | null;

    constructor(id?: number) {
        this.Id = id || 0;
        this.Type = 0;
        this.IncludeTenants = false;
        this.DeliveryConfig = new NotificationDeliveryConfig();
        this.Name = this.Email = this.RowVersion = "";
    }

    public get isNameValid(): boolean {
        return this.Name != null && this.Name.length > 0;
    }
  public get isEmailValid(): boolean {
    return this.Email != null && this.Email.length > 0 && Helper.EmailRegex.test(this.Email);
    }
    static Copy(obj: any): NotificationConfig {
        if (obj == null)
            return new NotificationConfig(0);

        let copy = new NotificationConfig(obj.Id || obj.id);
        copy.RowVersion = obj.RowVersion || obj.rowVersion || "";
        copy.Name = obj.Name || obj.name || "";
        copy.Email = obj.Email || obj.email || "";
        copy.Type = obj.Type || obj.type || 0;

        let deliveryConfig = obj.DeliveryConfig || obj.deliveryConfig;
        copy.DeliveryConfig = deliveryConfig == null ? null : NotificationDeliveryConfig.Copy(deliveryConfig);

        copy.TenantId = obj.TenantId || obj.id || 0;
        copy.TenantName = obj.TenantName || obj.tenantName || "";

        copy.IncludeTenants = obj.IncludeTenants || obj.includeTenants || false;

        return copy;
    }
}
