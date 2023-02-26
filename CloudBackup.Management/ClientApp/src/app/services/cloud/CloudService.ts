import { Injectable } from "@angular/core";
import { HttpService } from "../http/HttpService";

@Injectable({
    providedIn: 'root'
})
export class CloudService {
    private cloudUrl: string = '';

    constructor(private httpService: HttpService) {
        this.cloudUrl = httpService.baseUrl + "Cloud";
    }

    public getCloud(profileId: number) {
        return this.httpService.get(this.cloudUrl + `?profileId=${profileId}`);
    }
}