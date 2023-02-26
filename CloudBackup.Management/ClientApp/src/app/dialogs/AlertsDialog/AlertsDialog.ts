//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, ChangeDetectorRef, ViewChild } from '@angular/core';
//services
import { DxDataGridComponent } from 'devextreme-angular';
import { AlertType } from 'src/app/classes/Alert';
import { Alignment } from 'src/app/classes/Alignment';
import { DevExtremeHelper } from 'src/app/helpers/DevExtremeHelper';
import { AlertService } from 'src/app/services/alerts/AlertService';
import { HttpService } from 'src/app/services/http/HttpService';
import { environment } from 'src/environments/environment';
import { DataGridSettings } from '../../classes';
import { NotificationService } from "../../services/common/NotificationService";

@Component({
    selector: 'alerts-dialog',
    providers: [],
    templateUrl: './AlertsDialog.template.html'
})
@Injectable()
export class AlertsDialog implements OnInit {
    title = "Alerts";
    private socket: WebSocket;

    @ViewChild(DxDataGridComponent) grid: DxDataGridComponent;
    public selectedRows: Array<any>;
    public storageKey = 'alerts-grid';
    public gridsettings = new DataGridSettings();
    public alerts: any;
    public AlertType = AlertType; // to reference it from template

    get canMarkRead(): boolean {
        return this.selectedRows.some(x => x.isNew);
    }

    align: Alignment = "left";

    // popup
    @Input()
    visible: boolean;
    @Output()
    visibleChange = new EventEmitter<boolean>();

    public columns = [
        {
            caption: "Type",
            dataField: "type",
            width: "100",
            alignment: this.align,
            calculateCellValue: this.getAlertTypeCellValue,
            cellTemplate: "alertTypeIconTemplate"
        },
        {
            caption: "Date",
            dataField: "dateText",
            calculateSortValue: "date",
            width: "auto"
        },
        {
            caption: "Text",
            dataField: "message",
            width: "100%"
        }
    ];

    constructor(
        private notificator: NotificationService,
        private alertsService: AlertService,
        private changeDetector: ChangeDetectorRef,
        private httpService: HttpService
    ) {

        this.alerts = alertsService.alertsDataSource || [];
    }

    ngOnInit(): void {
        console.log('alerts: dialog ngInit');

        this.selectedRows = [];
    }

    onShow() {
        if (this.socket == null)
            this.initNotifications();

        this.selectedRows = [];

        this.changeDetector.detectChanges();
        this.visibleChange.emit(this.visible);

        this.update();
    }

    update = () => {
        this.grid.instance.refresh();
        console.log("alerts: refreshing grid...");
    }

    deleteAlerts() {
        this.notificator.confirmYesNo(this.doDeleteAlerts.bind(this), 'Are you sure you want to delete these alerts?');
    }

    markRead() {
        var alertIds = this.selectedRows.map((row: any) => row.id);

        console.log(`alerts: marking as read ${alertIds.length} alerts with ids=${alertIds.slice(0, 10).join(',')}...`);

        this.alertsService.markAlertsAsRead(alertIds)
            .then(() => {
                this.selectedRows = [];
                this.update();
            });
    }

    doDeleteAlerts(confirmed: boolean) {
        if (!confirmed)
            return;

        var alertIds = this.selectedRows.map((row: any) => row.id);

        console.log(`alerts: deleting ${alertIds.length} alerts with ids=${alertIds.slice(0, 10).join(',')}...`);

        this.notificator.setLoading(true, true);

        this.alertsService.deleteAlerts(alertIds)
            .then(() => {
                this.update();
                this.notificator.setLoading(false, true);
            });
    }

    private initNotifications = () => {
        var that = this;
        var port = environment.port; //location.port ? location.port : "443";
        var path = `${this.httpService.socketProtocol}//${window.location.hostname}${port ? ':' + port : ''}/alerts`;
        this.socket = new WebSocket(path);
        this.socket.binaryType = "blob";
        this.socket.onmessage = (ev: MessageEvent) => {
            if (ev.data === "NewAlerts") {
                this.update();
            }
        };
        this.socket.onclose = () => {
            setTimeout(function () {
                that.initNotifications();
            }, 5000);
        };
    }

    hide = () => {
        this.visible = false;
        this.changeDetector.detectChanges();
    }

    onHiding() {
        this.visibleChange.emit(false);
    }

    onSearchValueChanged(e: any) {
        this.grid.instance.searchByText(e.value);
    }

    onRowPrepared(rowInfo: any) {
        if (rowInfo.rowType !== 'header' && !rowInfo.data.isNew && !rowInfo.rowElement.classList.contains('light-secondary-text'))
            rowInfo.rowElement.classList.add('light-secondary-text');
    }

    getAlertTypeCellValue(data: any) {
        let icon: string = "";
        switch (data.type) {
            case AlertType.Info:
                icon = 'icon-info';
                break;
            case AlertType.Warning:
                icon = 'icon-warning';
                break;
            case AlertType.Error:
                icon = 'icon-error';
                break;
        }
        return { type: AlertType[data.type], "class": icon };
    }

    customLoad() {
        return DevExtremeHelper.loadGridState(this.storageKey);
    }

    customSave(state: any) {
        DevExtremeHelper.saveGridState(this.storageKey, state);
    }

    onRowClick(e: any) {
        if (e.rowType === 'data') {
            if (!this.grid.instance.isRowSelected(e.key)) {
                this.grid.instance.selectRows([e.key], true);
            } else {
                this.grid.instance.deselectRows([e.key]);
            }
        }
    }
}
