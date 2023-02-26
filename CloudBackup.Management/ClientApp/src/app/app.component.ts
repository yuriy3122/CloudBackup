import { PlatformLocation } from '@angular/common';
import { AfterContentChecked, Component, ChangeDetectorRef, } from '@angular/core';
import { ActivatedRoute, Route, Router } from '@angular/router';
import { Data } from 'popper.js';
import { BehaviorSubject, Observable } from 'rxjs';
import { AppRoutes } from './app.routes';
import { MessageList } from './classes/Message';
import { BreadCrumb, NavigationMenu, NavigationMenuItem } from './classes/NavigationMenu';
import { User } from './classes/User';
import { ConfirmDialogContext } from './dialogs/ConfirmDialog/ConfirmDialogContext';
import { CloudService } from './services/cloud/CloudService';
import { MenuService } from './services/common/MenuService';
import { NotificationService } from './services/common/NotificationService';
import { HttpService } from './services/http/HttpService';
//controllers

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html'
})
export class AppComponent implements AfterContentChecked {
  title = 'app';
  //data
  //menu
  private location: string;
  public menuCollapsed = false;
  public rightMenuCollapsed = false;
  //routes
  public activeMenuItem: Route | null;
  public activeRouteParent: Route | null;
  public activeRoute: Route = {};
  public childPath: any;
  private date = new Date();
  //loading spinner
  private loading: boolean;
  //navigation
  public navigationMenu: NavigationMenu | null = null;
  public navigationMenuData: Array<NavigationMenuItem> = [];
  public filteredNavigationItems: Array<NavigationMenuItem> = [];
  //breadcrumbs
  private breadcrumbsSubscription: Observable<BreadCrumb[]>;
  public breadcrumbs: Array<BreadCrumb>;
  //messages
  private messagesSubscription: Observable<MessageList>;
  private messages: MessageList;
  //suscriptions for data
  private locationSubscription: Observable<string>;
  //private routeSubscription: Observable<Map<string, ActivatedRoute>>;
  private childPathSubscription: Observable<any>;
  //auth data
  authorizedSubscription: any;
  authorized: boolean;
  //user
  currentUser: any;
  // confirm dialog
  private confirmDialogSubscription: Observable<ConfirmDialogContext>;
  public confirmDialogVisible = false;
  public confirmDialogContext: ConfirmDialogContext = new ConfirmDialogContext();

  constructor(
    public httpService: HttpService,
    private locator: PlatformLocation,
    private router: Router,
    public menu: MenuService,
    public notification: NotificationService,
    public changeDetector: ChangeDetectorRef  ) {

    let that = this;
    this.location = window.location.hash.replace("#/", "");
    this.currentUser = {};
    this.authorized = false;
    this.httpService.authorizedObserver.subscribe((value: boolean | null) => {
      if (value != null) {
        that.authorized = value;
        if (value) {
          that.httpService.get('/Administration/User/Current/').then((user: any) => {
            that.currentUser = user;
          });
        }
      }
    });
  
    let location = window.location.hash.replace("#/", "");
    this.loading = false;
    this.breadcrumbs = [];
    this.activeMenuItem = null;
    this.activeRouteParent = this.activeMenuItem;
    this.menu.firstLevelMenuChangeObserver.subscribe(activeMenuItem => {
      this.activeRouteParent = activeMenuItem;
    });
    this.locationSubscription = menu.getLocation();
    this.locationSubscription.subscribe(value => {
      if (value != null) {
        let menu = that.menu.getMenu(value);
        if (menu != null) {
          that.location = value;
          that.navigationMenu = menu;
          that.filteredNavigationItems = that.navigationMenu.data;
          that.navigationMenuData = that.navigationMenu.data;
          //that.menu.setRoute(that.navigationMenu.Route);
          that.menuCollapsed = true;
        } else
          that.menuCollapsed = false;
      }
    });

    this.breadcrumbsSubscription = this.menu.getBreadcrumbsObserver();
    this.breadcrumbsSubscription.subscribe(value => {
      if (value != null)
        this.breadcrumbs = value;
    });

    this.childPathSubscription = this.menu.getChildPathObserver();
    this.childPathSubscription.subscribe(value => {
      if (value != null && value[""] != null)
        that.childPath = value[""];
    });


    this.messagesSubscription = this.notification.getMessageObserver();
    this.messagesSubscription.subscribe(value => {
      if (value != null)
        that.messages = value;
    });

    this.confirmDialogSubscription = this.notification.getConfirmDialogObserver();
    this.confirmDialogSubscription.subscribe(context => {
      this.confirmDialogContext = context;
      this.confirmDialogVisible = true;
    });

    this.locator.onPopState(() => {
      this.ngAfterContentInit();
    });
  }
  ngAfterContentChecked() {
    this.changeDetector.detectChanges();
  }
  ngAfterContentInit() {
  }

  toggleCollapse() {
    this.rightMenuCollapsed = !this.rightMenuCollapsed;
  }

  checkRoutePermissions: (route: Route) => boolean = route => {
    return true;
  }

  logout() {
    this.httpService.setAuthorized(false);
    this.router.navigate(['/Login']);
  }
}
