import { Injectable } from '@angular/core';
import { BaseService } from '../common/BaseService';
import { HttpService } from '../http/HttpService';

@Injectable()
export class AuthService extends BaseService {
  constructor(private httpService: HttpService) {
    super();
    console.log('init permission service');
  }

  logon(request: string): Promise<any> {
    return this.httpService.post(this.loginUrl + "?credentialsEncoded=" + request).then(q => q);
  }

  isConfigured(): Promise<any>{
    return this.httpService.get(this.configuredUrl);
  }

  setConfiguration(config: string): Promise<any>{
    return this.httpService.post(this.configurationUrl, config);
  }
}
