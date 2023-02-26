import { Component, Injectable, Input, EventEmitter, Output } from '@angular/core';
import { Router, CanActivate } from '@angular/router';
import { HttpService } from '../services/http/HttpService';

@Injectable()
export class AuthenticationGuard implements CanActivate {

    constructor(private httpService: HttpService, private router: Router) { }

    canActivate(): boolean {
        if (this.httpService.token && this.httpService.tokenExpireDate > new Date()) {
            if (!this.httpService.authorized)
            this.httpService.setAuthorized(true);

            return true;
        }

        this.httpService.setAuthorized(false);
        this.router.navigate(['/Login']);

        return false;
    }
}