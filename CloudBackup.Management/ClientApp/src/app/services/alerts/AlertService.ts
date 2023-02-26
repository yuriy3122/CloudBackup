import { Alert } from '../../classes/Alert';
import { Injectable } from "@angular/core";
import { HttpService } from '../http/HttpService';
import { BaseService } from '../common/BaseService';

@Injectable()
export class AlertService extends BaseService {
    public alertsDataSource: any = {};
    public usersDataSource: any = {};
    public tenantsDataSource: any = {};
    public rolesDataSource: any = {};

    constructor(private httpService: HttpService) {
        super();
        this.alertsDataSource.store = this.getAlertsDataSource();
        console.log('init alerts service');
    }

    getAlerts(onlyNew?: boolean, minDate?: Date): Promise<Alert[]> {
        var query = '';
        if (onlyNew != null) {
            query += '&onlyNew=true';
        }
        if (minDate != null) {
            query += `&minDate=${minDate.toISOString()}`;
        }

        if (query !== '') {
            query = '?' + query.substring(1);
        }

        return this.httpService.get(this.alertsUrl + query)
            .then((q: any) => q.items.map((obj: any) => Alert.Copy(obj)));
    }
    getAlertsCountAsync(onlyNew?: boolean): Promise<number> {
        var query = this.alertsUrl + '/Count';
        if (onlyNew != null) {
            query += '?onlyNew=true';
        }

        return this.httpService.get(query).then((q: any) => q);
    }

    markAlertsAsRead(ids: Array<number>): Promise<any> {
        var serialized = JSON.stringify(ids);
        return this.httpService.post(this.alertsUrl + '/Read', serialized);
    }
    deleteAlert(id: number): Promise<any> {
        return this.httpService.delete(this.alertsUrl + "/" + id);
    }
    deleteAlerts(ids: Array<number>): Promise<any> {
        var serialized = JSON.stringify(ids);
        return this.httpService.post(this.alertsUrl + '/Delete', serialized);
    }
    getAlertsDataSource() {
        return this.httpService.getDataSource(this.alertsUrl);
    }
}