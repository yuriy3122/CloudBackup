import { Injectable, Input, EventEmitter, Output } from '@angular/core';
import { Route, ActivatedRoute } from "@angular/router";
import { BreadCrumb, NavigationMenu } from 'src/app/classes/NavigationMenu';

@Injectable({
    providedIn: 'root'
})
export class MenuService {
    private menuItems = new Map<string, NavigationMenu>(); //left menu items
    private location: string; //current location
    private currentRoute: any; 
    private currentChildRoute: any;
    public treelink: string; //active tree link
    public breadcrumbs: Array<BreadCrumb> = [];
    private childPath: any; //child path in menu

    //first level active menu
    private firstLevelActiveMenu: Route;

    //second level active menu
    private secondLevelActiveMenu: Route;

    @Output()
    public dataChangeObserver = new EventEmitter<Map<string, NavigationMenu>>();

    @Output()
    public locationChangeObserver = new EventEmitter<string>();

    @Output()
    public routeChangeObserver = new EventEmitter<Map<string, ActivatedRoute>>();

    @Output()
    public childRouteChangeObserver = new EventEmitter();

    @Output()
    public treelinkChangeObserver = new EventEmitter<string>();

    @Output()
    public breadcrumbsChangeObserver = new EventEmitter<BreadCrumb[]>();

    @Output()
    public childPathChangeObserver = new EventEmitter();

    //emmiter for first level menu
    @Output()
    public firstLevelMenuChangeObserver = new EventEmitter<Route>();

    //emmiter for second level menu
    @Output()
    public secondLevelMenuChangeObserver = new EventEmitter<Route>();

    constructor() {
        this.location = "";
        this.treelink = "";
        this.childPath = {};
        this.currentChildRoute = {};
        this.breadcrumbs = [];

        this.firstLevelActiveMenu = {};
        this.secondLevelActiveMenu = {};

        console.log('menu created');
    }
    
    @Input()
    public setNavigation(newList: NavigationMenu) {
        this.menuItems.set(newList.parentLink, newList);
        this.dataChangeObserver.emit(this.menuItems);
        return this.dataChangeObserver;
    }

    @Input()
    public setLocation(newLocation: string) {
        this.location = newLocation;
        this.locationChangeObserver.emit(this.location);

        // There is no menu now, so we'll reset current treelink
        if (!newLocation)
            this.setTreelink("");

        return this.locationChangeObserver;
    }

    @Input()
    public setRoute(newRoute: Route) {
        this.currentRoute = newRoute;
        this.routeChangeObserver.emit(this.currentRoute);
        //return this.routeChangeObserver;
    }

    @Input()
    public setChildRoute(newRoute: any) {
        this.currentChildRoute = newRoute;
        this.childRouteChangeObserver.emit(this.currentChildRoute);
        //return this.childRouteChangeObserver;
    }

    @Input()
    public setChildPath(newPath: any, path: string) {
        path = path || "";
        this.childPath[path] = newPath;
        this.childPath[""] = newPath;
        this.childPathChangeObserver.emit(this.childPath);
        return this.childPathChangeObserver;
    }


    @Input()
    public setTreelink(newTreelink: string) {
        this.treelink = newTreelink;
        this.treelinkChangeObserver.emit(this.treelink);
        return this.treelinkChangeObserver;
    }

    @Input()
    public addBreadcrumb(newCrumb: BreadCrumb) {
        this.breadcrumbs = this.breadcrumbs.slice(0, newCrumb.Rank);
        this.breadcrumbs.push(newCrumb);
        let compiledLink = "";
        for (let i = 0; i < this.breadcrumbs.length; i++)
        {
            if (i > 0) {
                compiledLink += this.breadcrumbs[i].Link;
                this.breadcrumbs[i].CompiledLink = compiledLink;
            } else
                this.breadcrumbs[i].CompiledLink = this.breadcrumbs[i].Link;
        }
        this.breadcrumbsChangeObserver.emit(this.breadcrumbs);
        return this.breadcrumbsChangeObserver;
    }

    //set first level menu active item
    @Input()
    public setFirstLevelMenu(menuItem: Route) {
        this.firstLevelActiveMenu = menuItem;
        this.firstLevelMenuChangeObserver.emit(this.firstLevelActiveMenu);
        return this.firstLevelMenuChangeObserver;
    }

    //set second level menu active item
    @Input()
    public setSecondLevelMenu(menuItem: Route) {
        this.secondLevelActiveMenu = menuItem;
        this.secondLevelMenuChangeObserver.emit(this.secondLevelActiveMenu);
        return this.secondLevelMenuChangeObserver;
    }

    public getNavigation() { return this.dataChangeObserver; } 
    public getLocation() { return this.locationChangeObserver; }
    public getCurrentRouteObserver() { return this.routeChangeObserver; }
    public getCurrentChildRouteObserver() { return this.routeChangeObserver; }
    public getTreelinkObserver() { return this.treelinkChangeObserver; }
    public getBreadcrumbsObserver() { return this.breadcrumbsChangeObserver; }
    public getChildPathObserver() { return this.childPathChangeObserver; }

    //first level menu observer
    public getFirstLevelMenuObserver() { return this.firstLevelMenuChangeObserver; }

    //second level menu observer
    public getSecondLevelMenuObserver() { return this.secondLevelMenuChangeObserver; }

    public getMenu(menuLocation: string) : NavigationMenu | undefined {
        return this.menuItems.get(menuLocation);
    }

    public getCurrentRoute() {
        return this.currentRoute;
    }

    public getCurrentChildRoute() {
        return this.currentChildRoute;
    }
}