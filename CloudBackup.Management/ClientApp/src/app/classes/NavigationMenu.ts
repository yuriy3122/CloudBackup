import { ActivatedRoute } from "@angular/router";

export class NavigationMenu {
    public parentLink: string;
    public data: Array<NavigationMenuItem>;
    public parentId: string;
    public route: ActivatedRoute | undefined;

    constructor(parentLink: string, data: Array<NavigationMenuItem>, route?: ActivatedRoute, parentId?: string) {
        this.parentLink = parentLink;
        this.data = data || [];
        this.parentId = parentId || "";
        this.route = route || undefined;
    }
}

export class NavigationMenuItem {
    public id: string;
    public text: string;
    public link: string;
    public child: string;
    private _text: string;
    public expanded: boolean;
    public selected: boolean;
    public item: any;
    public items: Array<NavigationMenuItem>;

    constructor(id: string, name: string, item: any, link: string, child: string, data?: Array<NavigationMenuItem>) {
        this.id = id;
        this.text = name;
        this.link = link;
        this.child = child;
        this._text = name.toLowerCase();
        this.item = item;
        this.items = data || [];
        this.expanded = true;
        this.selected = false;
    }

    toggle() {
        this.expanded = !this.expanded;
    }

    filter(text: string): boolean {
        return ((this.items == null ? false : this.items.some(function (item) { return item._text.indexOf(text) >= 0; })) || this._text.indexOf(text) >= 0);
    }
}

export class BreadCrumb {
    public Name: string;
    public Link: string;
    public CompiledLink: string;
    public Rank: number;

    constructor(name?: string, link?: string, rank?: number) {
        this.Name = name || "";
        this.Link = link || "";
        this.Rank = rank || 0;
        this.CompiledLink = "";
    }
}