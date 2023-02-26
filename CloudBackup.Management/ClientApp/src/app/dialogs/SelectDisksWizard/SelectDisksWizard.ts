//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, DoCheck, ViewChild, ChangeDetectorRef } from '@angular/core';
import { DxPopupComponent, DxDataGridComponent } from 'devextreme-angular';
//services
import { BackupService } from '../../services/backup/BackupService';
import { NotificationService } from '../../services/common/NotificationService';
//helper
import { Disk } from "../../classes/Disk";
import { CloudFolder } from 'src/app/classes/CloudFolder';
import { diskColumns } from 'src/app/classes/constants';
import { DataGridSettings } from '../../classes';

@Component({
    selector: 'select-disks-wizard',
    templateUrl: './SelectDisksWizard.template.html'
})

@Injectable()
export class SelectDisksWizard implements OnInit {
    public gridsettings = new DataGridSettings();
    //clouds and folders
    clouds: Array<any> = [];
    selectedCloud!: string;

    folders: Array<CloudFolder> = [];
    selectedFolderId!: string;

    //popup
    @ViewChild(DxPopupComponent)
    popup!: DxPopupComponent;
    //current disks
    public disks: any;
    @Input()
    public selectedDisks!: Disk[];
    public selectedKeys!: Disk[];
    public selectedDisksObserver = this.backupService.getDiskObserver();
    //show popup or not
    @Input()
    public popupVisible!: boolean;
    @Output() popupVisibleChange = new EventEmitter();
    @Output() onSelectedDisks = new EventEmitter<Disk[]>();
    @Output() selectedDisksChange = new EventEmitter<Disk[]>();
    @Input()
    public wizardGuid!: string;

    @Input()
    public tenantId!: number;

    //grid
    @ViewChild(DxDataGridComponent)
    grid!: DxDataGridComponent;
    public columns = diskColumns;

    constructor(
        public notificator: NotificationService,
        public backupService: BackupService,
        public changeDetector: ChangeDetectorRef) {
    }

    // ngDoCheck() {
    //     this.popupVisibleChange.emit(this.popupVisible);
    //     this.selectedDisksChange.emit(this.selectedDisks);
    //     this.selectedDisksObserver.emit(this.selectedDisks);
    // }

    ngOnInit() {
        console.log('select disks wizard ngInit');
    }

    onShow() {
        this.disks = [];
        this.selectedKeys = Object.assign([], this.selectedDisks || []);

        Promise.all([this.getClouds()]).then(() => this.onValueChanged());
    }

    onSearchValueChanged(e: any) {
        this.grid.instance.searchByText(e.value);
    }

    getClouds = () => {
        this.backupService.getClouds().then(clouds => {
            //this.clouds = clouds.map((x: any) => x.name);
            this.clouds = clouds;
            console.log(this.clouds);
            if (this.clouds && this.clouds.length === 1)
                this.selectedCloud = this.clouds[0].id;
        });
    }

    getFolders = () => {
        if (this.selectedCloud != null) {
            this.backupService.getFolders(this.selectedCloud).then((folders: Array<CloudFolder>) => {
                this.folders = folders;
                if (this.folders && this.folders.length === 1)
                    this.selectedFolderId = this.folders[0].id || "";
            });
        }
    }

    onCloudChanged() {
        this.disks = [];

        if (this.selectedCloud != null) {
            this.backupService.setSelectedCloud(this.selectedCloud);
            this.getFolders();
        }
    }

    onValueChanged() {
        if (this.selectedFolderId != null) {
            this.backupService.setSelectedFolder(this.selectedFolderId);
        }

        if (this.selectedFolderId != null && this.selectedCloud != null) {
            if (this.folders.some(x => x.id === this.selectedFolderId)) {
                this.disks = this.backupService.getDisksDataSource(this.selectedFolderId);
            }
        }

        this.changeDetector.detectChanges();
    }

    repaintPopup() {
        this.popup.instance.repaint();
    }

    onHiding() {
        this.popupVisibleChange.emit(false);
    }

    onSelectionChanged(e: any) {
    }

    save = () => {
        const selectedDisks = this.selectedKeys.slice();

        this.onSelectedDisks.emit(selectedDisks);
        this.selectedDisksChange.emit(selectedDisks);
        var diskDescList = selectedDisks
            .map((item: any) => {
                return { disk: item, guid: this.wizardGuid }
            });

        this.selectedDisksObserver.emit(diskDescList);
    }

    hide = () => {
        this.popupVisible = false;
        this.changeDetector.detectChanges();
    }

    ok = () => {
        this.save();
        this.hide();
    }

    customizeColumns(columns: any) {
        for (var i = 0; i < columns.length; i++) {
            columns[i].width = "auto";
        }
    }

    onRowClick(e: any) {
        if (e.rowType === 'data') {
            if (e.isSelected) {
                this.grid.instance.deselectRows([e.key]);
            } else {
                this.grid.instance.selectRows([e.key], true);
            }
        }
    }
}
