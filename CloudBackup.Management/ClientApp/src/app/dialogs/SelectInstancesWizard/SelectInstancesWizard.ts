//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, DoCheck, ViewChild, ChangeDetectorRef } from '@angular/core';
import { DxPopupComponent, DxDataGridComponent } from 'devextreme-angular';
//services
import { BackupService } from '../../services/backup/BackupService';
import { NotificationService } from '../../services/common/NotificationService';
//profile
import { CloudFolder } from "../../classes/CloudFolder";
import { instanceColumns } from 'src/app/classes/constants';
import { DataGridSettings } from '../../classes';
import { Helper } from 'src/app/helpers/Helper';

@Component({
    selector: 'select-instances-wizard',
    templateUrl: './SelectInstancesWizard.template.html'
})

@Injectable()
export class SelectInstancesWizard implements OnInit {
    public gridsettings = new DataGridSettings();
    //basic data
    public buttonOKText = "Add";
    public okButton: any;
    public buttonCancelText = "Cancel";
    public cancelButton: any;
    //clouds and folders
    clouds: Array<any> = [];
    selectedCloud!: string;

    folders: Array<CloudFolder> = [];
    selectedFolderId!: string;

    //popup
    @ViewChild(DxPopupComponent)
    popup!: DxPopupComponent;
    //all instances that we have in list
    public instances: any;

    //instances that are selected
    @Input()
    public selectedInstances: any[];
    @Output()
    public selectedInstancesChange = new EventEmitter<any[]>();
    @Output() onSelectedInstance = new EventEmitter<any[]>();
    public selectedKeys!: any[];
    public selectedInstacesObserver = this.backupService.getInstanceObserver();
    //show popup or not
    @Input()
    public popupVisible!: boolean;
    @Output() popupVisibleChange = new EventEmitter();

    @Input()
    public wizardGuid!: string;

    @Input()
    public tenantId!: number;

    // grid
    @ViewChild(DxDataGridComponent)
    grid!: DxDataGridComponent;
    public columns = instanceColumns;

    constructor(
        public notificator: NotificationService,
        public backupService: BackupService,
        public changeDetector: ChangeDetectorRef) {

        //this.selectedInstances = [];
    }

    ngOnInit() {
        console.log('select instances wizard ngInit');
    }

    onShow() {
        this.instances = [];
        this.selectedKeys = Object.assign([], this.selectedInstances || []);
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

    repaintPopup() {
        this.popup.instance.repaint();
    }

    onHiding() {
        this.popupVisibleChange.emit(false);
    }

    onSelectionChanged(e: any) {
    }

    ok = () => {
        this.save();
        this.hide();
    }

    save = () => {
        const selectedInstances = this.selectedKeys.slice();
        this.selectedInstancesChange.emit(selectedInstances);
        this.onSelectedInstance.emit(selectedInstances);

        var instanceDescList = selectedInstances
            .map((item: any) => {
                return { instance: item, guid: this.wizardGuid }
            });

        this.selectedInstacesObserver.emit(instanceDescList);
    }

    hide = () => {
        this.popupVisible = false;
        this.changeDetector.detectChanges();
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

    onCloudChanged() {
        this.instances = [];

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
                this.instances = this.backupService.getInstancesDataSource(this.selectedFolderId);
            }
        }

        this.changeDetector.detectChanges();
    }
}
