import { ChangeDetectorRef, Component } from '@angular/core';
import { Route } from '@angular/router';
import { AppRoutes } from 'src/app/app.routes';
import { MenuService } from 'src/app/services/common/MenuService';
import { BaseMenuComponent } from '../base/base-menu.component';

@Component({
  selector: 'main-menu',
  templateUrl: './main-menu.component.html'
})
export class MainMenuComponent extends BaseMenuComponent {
  public leftMenu: Route[];
  firstLevelMenuSubscription: any;
  currentRoute: any;

  constructor(
    public menu: MenuService,
    private changeDetector: ChangeDetectorRef
  ) {
    super();
    this.firstLevelMenuSubscription = this.menu.getFirstLevelMenuObserver();
  }

  setCurrentRoute(route: Route) {
    this.menu.setRoute(route);
  }

  ngOnInit() {
    this.leftMenu = AppRoutes.leftMenu.filter(this.checkRoutePermissions);
    this.firstLevelMenuSubscription.subscribe((value: null) => {
      if (value != null) {
        this.currentRoute = value;
        this.changeDetector.detectChanges();
      }
    });
  }
}
