import { ChangeDetectorRef, Component } from '@angular/core';
import { Route } from '@angular/router';
import { MenuService } from 'src/app/services/common/MenuService';
import { BaseMenuComponent } from '../base/base-menu.component';

@Component({
  selector: 'secondary-menu',
  templateUrl: './secondary-menu.component.html',
  styleUrls: ['./secondary-meny.styles.scss']
})
export class SecondaryMenuComponent extends BaseMenuComponent {
  isExpanded = false;
  routeSubscription: any;
  currentRoute: Route | null;
  parent: any;

  constructor(
    public menu: MenuService,
    private changeDetector: ChangeDetectorRef
  ) {
    super();
    this.currentRoute = null;
    let that = this;

    this.routeSubscription = this.menu.getFirstLevelMenuObserver();
    this.routeSubscription.subscribe((value: null) => {
      if (value != null) {
        that.currentRoute = value;
        this.changeDetector.detectChanges();
      }
    });
  }

  setActiveTab(tab: any) {
    this.menu.setChildPath(null, "");
    this.menu.setRoute(tab);
  }
}
