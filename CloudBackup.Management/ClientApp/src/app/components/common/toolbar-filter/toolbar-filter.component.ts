import { Component, ViewChild, ElementRef, ChangeDetectorRef, Input, Output, EventEmitter, SimpleChanges, HostListener } from '@angular/core';

@Component({
    selector: 'toolbar-filter',
    templateUrl: './toolbar-filter.template.html',
    styleUrls: ['./toolbar-filter.component.scss'],
    host: { 'style': 'display: inline-block;' }
})
export class ToolbarFilterComponent {
    private initialized = false;

    @ViewChild('filterButtonContainer') filterButton: ElementRef;
    //@ViewChild('filterContainer') filterContainer: ElementRef;

    @Input()
    filterPanel: HTMLElement;

    @Input()
    filterVisible = false;
    @Output()
    filterVisibleChange = new EventEmitter<boolean>();

    offset: number;

    constructor(private changeDetector: ChangeDetectorRef) {

    }

    ngAfterViewInit() {
        if (this.filterPanel)
            this.filterPanel.classList.add("container-wrapper");

        this.refresh();

        this.initialized = true;
    }

    ngOnDestroy() {
        if (this.filterPanel)
            this.filterPanel.classList.remove("container-wrapper");
    }

    ngOnChanges(e: SimpleChanges) {
        if (e.filterVisible != null && this.initialized && this.filterPanel) {
            this.refresh();
        }
    }

    changeVisible() {
        this.filterVisible = !this.filterVisible;
        this.filterVisibleChange.emit(this.filterVisible);
        this.changeDetector.detectChanges();

        this.filterPanel.classList.toggle("hide");

        this.refresh();
    }

    @HostListener('window:resize')
    onResize() {
        this.refresh();
    }

    private refresh() {
        if (!this.filterPanel)
            return;

        if (!this.filterVisible)
            this.filterPanel.classList.add("hide");
        else
            this.filterPanel.classList.remove("hide");

        //this.filterPanel.style.minWidth =
        //    this.filterButton.nativeElement.offsetLeft +
        //    this.filterButton.nativeElement.offsetWidth -
        //    this.filterPanel.offsetLeft + 'px';
        
        this.refreshOffset();
    }

    private refreshOffset() {
        this.offset = this.filterVisible && this.filterPanel && this.filterButton
            ? (this.filterPanel.offsetTop -
                this.filterButton.nativeElement.offsetTop -
                this.filterButton.nativeElement.offsetHeight + 2)
            : 0;
        this.changeDetector.detectChanges();
    }
}
