import { Injectable, EventEmitter, Output } from '@angular/core';
import { User } from 'src/app/classes/User';
import { UserPermissions } from 'src/app/classes/UserPermissions';
import { HttpService } from '../http/HttpService';
import { BaseService } from './BaseService';

@Injectable()
export class PermissionService extends BaseService {
  constructor(private httpService: HttpService) {
    super();
    console.log('init permission service');
  }

  getCurrentUser(): Promise<User> {
    return this.httpService.get(this.usersUrl + '/Current').then(q => User.Copy(q));
  }

  // permissions
  getPermissions(): Promise<UserPermissions> {
    return this.httpService.get(this.permissionsUrl).then(q => UserPermissions.Copy(q));
  }
}
