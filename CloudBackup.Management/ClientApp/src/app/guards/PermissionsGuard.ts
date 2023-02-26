import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRouteSnapshot, Data } from '@angular/router';
import { UserPermissions } from '../classes/UserPermissions';
import { BackupService } from '../services/backup/BackupService';
import { PermissionService } from '../services/common/PermissionService';
import { HttpService } from '../services/http/HttpService';

@Injectable()
export class PermissionsGuard implements CanActivate {
    private static permissions: UserPermissions | null;
    private static isWafAvailable: boolean;

    constructor(protected router: Router,
        protected permissionService: PermissionService,
        protected backupService: BackupService,
        protected httpService: HttpService) { }

    canActivate(route: ActivatedRouteSnapshot): Promise<boolean> {
      return Promise.resolve(true);
    }

    public static onLogout() {
        //clear old permissions after logout
        PermissionsGuard.permissions = null;        
    }

    public static checkRoutePermissions(routeData: Data, permissions: UserPermissions, isWafAvailable: boolean): boolean {
        if (typeof (routeData.hasPermissions) == typeof (Function)) {
          return true;
        }

        return true;
    }
}
