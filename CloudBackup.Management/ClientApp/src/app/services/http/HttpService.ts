//inner
import { Injectable, EventEmitter, Output } from '@angular/core';
import { HttpHeaders, HttpClient, HttpResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { defaultPageSize } from '../../classes/constants';
import { Observable, ObservableInput } from 'rxjs';
import { Helper } from 'src/app/helpers/Helper';
import CustomStore from 'devextreme/data/custom_store';
import { User } from 'src/app/classes/User';

// inject all services in 'root' so they will be created as singleton
@Injectable({
    providedIn: 'root'
})
export class HttpService {
    private headers: HttpHeaders = new HttpHeaders();
    authorized: boolean = false;
    public currentUser: User = new User();

    get token(): string {
        return localStorage["auth_token"];
    };
    set token(token: string) {
        localStorage["auth_token"] = token;
    }

    get tokenExpireDate(): Date {
        var date = localStorage["token_date"];
        return date != null ? new Date(JSON.parse(date)) : new Date(0, 0, 0);
    }
    set tokenExpireDate(date: Date | null) {
        localStorage["token_date"] = date != null ? JSON.stringify(date) : null;
    }

    @Output()
    public authorizedObserver = new EventEmitter<boolean>();

    //, private notificator: NotificationService
    constructor(private client: HttpClient, private router: Router) {
        this.authorizedObserver = new EventEmitter<boolean>();
        //HttpController.tenants = [];
        //this.headers = this.headers.set('Authorization', 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjpbInl1cml5IiwieXVyaXkiXSwic3ViIjoieXVyaXkiLCJqdGkiOiI2Y2M0NzE5Zi1lNmFhLTRlZmQtYTI3MC1iODcyOGQ2OTI1YzkiLCJpYXQiOjE2NTM0NzgwMzQsIm5iZiI6MTY1MzQ3ODAzNCwiZXhwIjoxNjU2MDcwMDM0LCJpc3MiOiJZYW5kZXhCYWNrdXBJc3N1ZXIiLCJhdWQiOiJZYW5kZXhCYWNrdXBBdWRpZW5jZSJ9.fuWlEoOvrmalryrv2uuKdrpg-M5hY7qw0SAC8SEeqBU');
        //this.tokenExpireDate = new Date(2123, 1, 1);
        //this.token = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjpbInl1cml5IiwieXVyaXkiXSwic3ViIjoieXVyaXkiLCJqdGkiOiI2Y2M0NzE5Zi1lNmFhLTRlZmQtYTI3MC1iODcyOGQ2OTI1YzkiLCJpYXQiOjE2NTM0NzgwMzQsIm5iZiI6MTY1MzQ3ODAzNCwiZXhwIjoxNjU2MDcwMDM0LCJpc3MiOiJZYW5kZXhCYWNrdXBJc3N1ZXIiLCJhdWQiOiJZYW5kZXhCYWNrdXBBdWRpZW5jZSJ9.fuWlEoOvrmalryrv2uuKdrpg-M5hY7qw0SAC8SEeqBU';
        console.log('http controller init');
    }


  public setAuthorized(value: boolean) {
        console.log("setAuth: " + value);
        this.headers = new HttpHeaders({ 'Content-Type': 'application/json' });
        if (value) {
            this.headers = this.headers.set('Authorization', 'Bearer ' + this.token);
            //this.headers.append('Authorization', 'bearer ' + this.token);
        } else {
            this.token = "";
            this.tokenExpireDate = null;
        }

        this.authorized = value;
        this.authorizedObserver.emit(this.authorized);
    }

    public get protocol(): string { return window.location.protocol; }
    public get socketProtocol(): string { return this.protocol === "http:" ? "ws:" : "wss:"; }
    get baseUrl(): string { return this._baseUrl; }

    private hostUrl = (window.location.origin != null && window.location.origin.length > 0) ? window.location.origin :
        window.location.protocol + '//' + window.location.hostname + (window.location.port ? (':' + window.location.port) : '');

    private _baseUrl = this.hostUrl;

    //private 
    private getErrorMessage(error: any) {
        let message = Helper.GetErrorMessage(error);

        return message || "Data Loading Error";
    }

    //---------------------------------------------------
    getDataSource(url: string, key?: string | Array<string>) {
        let that = this;
        return new CustomStore({
            key: key,
            load: (loadOptions) => {
                let params = new Array<string>();

                if (loadOptions.take || loadOptions.skip) {
                    var skip = loadOptions.skip || 0;
                    var take = loadOptions.take || defaultPageSize;
                    var pageNum = Math.floor(skip / take) + 1;
                    params.push('pageNum=' + pageNum);
                    params.push('pageSize=' + take);
                }

                if (loadOptions.sort) {
                    const sort = loadOptions.sort as any;
                    if (Array.isArray(sort)) {
                        let order = sort[0].selector;
                        if (sort[0].desc) {
                            order += '[desc]';
                        }
                        params.push('order=' + order);
                    } else {
                        let order = sort.selector;
                        if (sort.desc) {
                            order += '[desc]';
                        }
                        params.push('order=' + order);
                    }
                }
                if (loadOptions.filter) {
                    if (Array.isArray(loadOptions.filter[0])) {
                        params.push('filter=' + loadOptions.filter[0][2]);
                    } else {
                        params.push('filter=' + loadOptions.filter[2]);
                    }
                }

                //params.push('_=' + new Date().getTime());

                let urlWithParams = url + (url.indexOf('?') > 0 ? '&' : '?') + params.join('&');

                return that.get(urlWithParams)
                    .then((result: any) => {
                        return {
                            data: result.items,
                            totalCount: result.totalCount
                        }
                    })
                    .catch(error => { throw that.getErrorMessage(error); });
            }
        });
    }

    //base methods
    get(url: string) {
        const promise = new Promise<void>((resolve, reject) => {
            const _ = (url.indexOf("?") < 0 ? "?" : "&") + "_=" + new Date().getTime();
            this.client.get(this._baseUrl + url + _,
                { headers: this.headers }).subscribe({
                    next: (res: any) => {
                        resolve(res);
                    },
                    error: (err: any) => {
                        this.processError(err);
                        reject(err);
                    },
                    complete: () => {
                        console.log('complete');
                    },
                });
        });
        return promise;

        //const _ = (url.indexOf("?") < 0 ? "?" : "&") + "_=" + new Date().getTime();
        //return this.client.get(url + _, { headers: this.headers }).catch(this.processError).toPromise();
    }

    downloadFile(url: string) {
        const promise = new Promise<void>((resolve, reject) => {
            const _ = (url.indexOf("?") < 0 ? "?" : "&") + "_=" + new Date().getTime();
            this.client.get(this._baseUrl + url + _,
                { headers: this.headers, responseType: 'blob' }).subscribe({
                    next: (res: any) => {
                        resolve(res);
                    },
                    error: (err: any) => {
                        this.processError(err);
                        reject(err);
                    },
                    complete: () => {
                        console.log('complete');
                    },
                });
        });
        return promise;
    }

    post(url: string, data?: any, options?: any) {
        if (options == null) {
            let headers = this.headers;
            if (headers)
                headers.append("Content-Type", "application/json");
            else
                headers = new HttpHeaders({ 'Content-Type': 'application/json' });

            options = { headers: headers };
        }

        const promise = new Promise<void>((resolve, reject) => {
            this.client.post(this._baseUrl + url, data, options).subscribe({
                next: (res: any) => {
                    resolve(res);
                },
                error: (err: any) => {
                    this.processError(err);
                    reject(err);
                },
                complete: () => {
                    console.log('complete');
                },
            });
        });
        return promise;

        //return this.client.post(url, data, options).catch(this.processError).toPromise();
    }

    put(url: string, data?: any) {
        const promise = new Promise<void>((resolve, reject) => {
            this.client.put(this._baseUrl + url, data, { headers: this.headers }).subscribe({
                next: (res: any) => {
                    resolve(res);
                },
                error: (err: any) => {
                    this.processError(err);
                    reject(err);
                },
                complete: () => {
                    console.log('complete');
                },
            });
        });
        return promise;

        //return this.client.put(url, data, { headers: this.headers }).catch(this.processError).toPromise();
    }

    delete(url: string) {
        const promise = new Promise<void>((resolve, reject) => {
            this.client.delete(this._baseUrl + url, { headers: this.headers }).subscribe({
                next: (res: any) => {
                    resolve(res);
                },
                error: (err: any) => {
                    this.processError(err);
                    reject(err);
                },
                complete: () => {
                    console.log('complete');
                },
            });
        });
        return promise;
        //return this.client.delete(url, { headers: this.headers }).catch(this.processError).toPromise();
    }

    patch(url: string, data?: any) {
        const promise = new Promise<void>((resolve, reject) => {
            this.client.patch(this._baseUrl + url, data, { headers: this.headers }).subscribe({
                next: (res: any) => {
                    resolve(res);
                },
                error: (err: any) => {
                    this.processError(err);
                    reject(err);
                },
                complete: () => {
                    console.log('complete');
                },
            });
        });
        return promise;
        //return this.client.patch(url, data, { headers: this.headers }).catch(this.processError).toPromise();
    }

    //: ObservableInput<Response>
    private processError = (error: any, response?: Observable<any>) => {
        if (error.status == 401) {
            this.setAuthorized(false);
            //this.router.navigate(['/' + AppRoutes.LoginRoute.path]);
        }

        if (error.status != 200)
            console.log(error.responseJSON);

        //add error message handler/notifications here
        //var message = Message.Error(error.responseJSON, "error");
        //this.notificator.addMessage(message);

        //throw new Error(this.getErrorMessage(error));
    }
}
