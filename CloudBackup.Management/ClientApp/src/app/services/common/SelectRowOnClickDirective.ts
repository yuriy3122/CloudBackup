import { Directive, HostListener, Self } from '@angular/core';
import { DxDataGridComponent } from "devextreme-angular";

@Directive({
    selector: '[selectRowOnClick]'
})
export class SelectRowOnClickDirective{
    constructor(@Self() private grid: DxDataGridComponent) {}

    ngAfterViewInit() {
        if (this.grid != null && this.grid.instance != null) {
            this.grid.instance.on("rowClick", this.onRowClick);
        }
    }

    private onRowClick = (e: any) => {
        if (e.rowType === 'data') {
            if (e.isSelected) {
                this.grid.instance.deselectRows([e.key]);
            } else {
                this.grid.instance.selectRows([e.key], true);
            }
        }
    }
}