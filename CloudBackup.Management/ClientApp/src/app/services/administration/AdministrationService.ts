import { Injectable } from '@angular/core';
import { HttpService } from '../http/HttpService';
import { Tenant } from '../../classes/Tenant';
import { BaseService } from '../common/BaseService';
import { Profile } from 'src/app/classes/Profile';
import { User } from '../../classes/User';
import { UtcOffset } from '../../classes/UtcOffset';
import { Role } from '../../classes/Role';
import { NotificationConfig } from '../../classes/NotificationConfig';

@Injectable({
    providedIn: 'root'
})
export class AdministrationService extends BaseService {
    constructor(private httpService: HttpService) {
        super();
    }

    checkLoginAsync(login: string): Promise<boolean> {
        var encodedLogin = encodeURIComponent(login);
        return this.httpService.get(this.usersUrl + '/CheckLogin/' + encodedLogin).then((q: any) => q);
    }

    getUtcOffsets(): Promise<UtcOffset[]> {
        return this.httpService.get(this.baseUrl + "UtcOffsets")
            .then((q: any) => q.map((obj: any) => UtcOffset.Copy(obj)));
    }

    getRoles(): Promise<Role[]> {
        return this.httpService.get(this.rolesUrl)
            .then((q: any) => q.items.map((obj: any) => Role.Copy(obj)));
    }
    
    //profiles
    getProfilesDataSource() {
        return this.httpService.getDataSource(this.profilesUrl, "id");
    }

    deleteProfile(id: number) {
        return this.httpService.delete(this.profilesUrl + "/" + id);
    }

    addProfile(profile: Profile): Promise<Profile> {
        var serialized = JSON.stringify(profile);
        return this.httpService.post(this.profilesUrl, serialized)
            .then(q => Profile.Copy(q));
    }

    updateProfile(profile: Profile): Promise<any> {
        var temp = new Profile();
        temp.Id = profile.Id;
        temp.RowVersion = profile.RowVersion;
        temp.Name = profile.Name;
        temp.KeyId = profile.KeyId;
        temp.PrivateKey = profile.PrivateKey;
        temp.ServiceAccountId = profile.ServiceAccountId;

        var serialized = JSON.stringify(temp);

        return this.httpService.put(this.profilesUrl + "/" + profile.Id, serialized);
    }

    //tenants
    getAllowedTenants(): Promise<Tenant[]> {
        return this.httpService.get(this.tenantsUrl + '/Allowed').then((q: any) => q.items.map((obj: any) => Tenant.Copy(obj)));
    }

    getTenantsDataSource() {
        return this.httpService.getDataSource(this.tenantsUrl, "id");
    }

    deleteTenant(id: number) {
        return this.httpService.delete(this.tenantsUrl + "/" + id);
    }

    addTenant(tenant: Tenant): Promise<Tenant> {
        var serialized = JSON.stringify(tenant);
        return this.httpService.post(this.tenantsUrl, serialized)
          .then(q => Tenant.Copy(q));
    }

    updateTenant(tenant: Tenant): Promise<any> {
        var serialized = JSON.stringify(tenant);
        return this.httpService.put(this.tenantsUrl + "/" + tenant.Id, serialized);
    }

    //users
    getUsersDataSource() {
        return this.httpService.getDataSource(this.usersUrl, "id");
    }

    deleteUser(id: number) {
        return this.httpService.delete(this.usersUrl + "/" + id);
    }

    addUser(user: User): Promise<User> {
        var serialized = JSON.stringify(user);
        return this.httpService.post(this.usersUrl, serialized)
          .then(q => User.Copy(q));
    }

    updateUser(user: User): Promise<any> {
        var serialized = JSON.stringify(user);
        return this.httpService.put(this.usersUrl + "/" + user.Id, serialized);
    }

    //notifications
    getNotificationsDataSource() {
        return this.httpService.getDataSource(this.notificationsUrl, "id");
    }

    addNotificationConfig(config: NotificationConfig): Promise<NotificationConfig> {
        var serialized = JSON.stringify(config);
        return this.httpService.post(this.notificationsUrl, serialized)
            .then(q => NotificationConfig.Copy(q));
    }

    updateNotificationConfig(config: NotificationConfig) {
        var serialized = JSON.stringify(config);
        return this.httpService.put(this.notificationsUrl + "/" + config.Id, serialized);
    }

    deleteNotification(ids: Array<number>) {
        var serialized = JSON.stringify(ids);
        return this.httpService.delete(this.notificationsUrl + '?configIds=' + serialized);
    }
}
