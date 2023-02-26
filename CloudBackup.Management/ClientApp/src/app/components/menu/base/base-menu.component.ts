import { Route } from "@angular/router";

export class BaseMenuComponent {
    checkRoutePermissions: (route: Route) => boolean = route => {
        return true;
    }
}
