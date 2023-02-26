import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CustomDate } from '../../classes/CustomDate';
import { Helper } from "../../helpers/Helper";
import { AuthService } from 'src/app/services/auth/AuthService';
import { HttpService } from 'src/app/services/http/HttpService';

@Component({
  templateUrl: './login.template.html',
  styleUrls: ['./login.styles.scss']
})
export class LoginComponent {
  static path: "Login";
  //login
  public buttonCancelText = "CANCEL";
  public buttonLoginText = "LOGIN";
  public rememberMeText = "Remember me";
  public login: string;
  public password: string;
  public cancelButton: any;
  public loginButton: any;
  public remember: any;
  public showSpinner: boolean;
  loginErrorMessage = "";
  passwordErrorMessage = "";
  //if app is already registered
  public registered: boolean;
  //todo: replace with dummy login
  public dummyLogin = "Root_admin";//"dummyLogin";
  public dummyPassword = "dummyPassword";

  //login popup
  constructor(private router: Router, private authService: AuthService, private httpService: HttpService) {
    this.remember = false;
    this.showSpinner = false;
    this.registered = true;

    let that = this;
    this.authService.isConfigured().then((result: any) => {
      if (result.IsConfigured || result.isConfigured) {
        that.registered = true;
        return true;
      }
      else {
        that.router.navigate(['/Registration']);
        return false;
      }
    }, (error) => {
      console.log(error);
    });
  }

  loginEnd() {
    this.loginErrorMessage = "";
    this.passwordErrorMessage = "";

    if (!this.login) {
      this.loginErrorMessage = "Login cannot be empty";
      return;
    }
    if (!Helper.AlphaNumericNotStartsWithNumberRegex.test(this.login)) {
      this.loginErrorMessage = "Login may only contain latin characters, numbers and underscores and cannot start with a number";
      return;
    }
    if (!Helper.PasswordRegex.test(this.password)) {
      this.passwordErrorMessage = "Password must contain only latin characters, numbers or special symbols";
      return;
    }

    this.showSpinner = true;
    const request = btoa(JSON.stringify({ username: this.login || this.dummyLogin, password: this.password || this.dummyPassword }));
    this.authService.logon(request).then((token) => {
      const tokenLifeTimeSeconds = this.remember ? token.expires_in : 24 * 60 * 60; // 1 day
      localStorage["auth_token"] = token.access_token;
      localStorage["token_date"] = JSON.stringify(new CustomDate().add(tokenLifeTimeSeconds, 'seconds').getDate());
      this.httpService.token = token.access_token;
      this.httpService.tokenExpireDate = new CustomDate().add(tokenLifeTimeSeconds, 'seconds').getDate();
      this.httpService.setAuthorized(true);
      this.router.navigate(['/']);
      this.showSpinner = false;
      console.log(token);
    }, (error: any) => {
      console.log(error);
      this.showSpinner = false;
      this.passwordErrorMessage = "The username or password is not valid";
    });
  }

  clear() {
    this.login = "";
    this.password = "";
  }

  toggleSpinner() {
    this.showSpinner = !this.showSpinner;
  }

  onKeyPressed(e: any) {
    if (e.key === "Enter") {
      this.loginEnd();
    }
  }
}
