import { Component, Injectable, OnInit } from '@angular/core';
import { Alert, AlertType } from 'src/app/classes/Alert';
import { AlertService } from 'src/app/services/alerts/AlertService';
import { NotificationService } from 'src/app/services/common/NotificationService';

@Component({
    selector: 'alerts',
    template: `
        <span style="display:block; float:rigth;" (click)="alertsDialogVisible = true"  class="navbar-alerts"> <b>{{alertsCount}}</b></span>
            <alerts-dialog id="alerts-dialog" [(visible)]="alertsDialogVisible" (visibleChange)="onDialogVisibleChange()">
            </alerts-dialog>`
})
@Injectable()
export class AlertsComponent implements OnInit {
    socket: WebSocket;

    alertsCount: number;
    private lastCheck: Date;
    alertsDialogVisible = false;

    constructor(
        private readonly alertService: AlertService,
        private readonly notificator: NotificationService) {
    }

    ngOnInit(): void {
        this.lastCheck = new Date();
        this.update();
        this.initNotifications();
    }

    ngOnDestroy() {
        this.socket.onclose = null;
        this.socket.close();
    }

    update() {
        this.alertService.getAlertsCountAsync(true).then(count => { this.alertsCount = count });
    }

    onDialogVisibleChange() {
        if (!this.alertsDialogVisible)
            this.update();
    }

    private onNewAlerts = () => {
        this.update();

        this.alertService.getAlerts(true, this.lastCheck)
            .then(alerts => {
                for (var alert of alerts) {
                    this.showNotification(alert);
                }
            });
        this.lastCheck = new Date();
    }

    private showNotification(alert: Alert) {
        switch (alert.Type) {
            case AlertType.Info:
                this.notificator.showInfoNotification(alert.Message);
                break;
            case AlertType.Error:
                this.notificator.showErrorNotification(alert.Message);
                break;
            case AlertType.Warning:
                this.notificator.showWarningNotification(alert.Message);
                break;
        }
    }

    private initNotifications = () => {
        this.socket = this.notificator.getSocket("alerts");
        this.socket.onmessage = (ev: MessageEvent) => {
            if (ev.data === "NewAlerts") {
                this.onNewAlerts();
            }
        };
        this.socket.onclose = () => setTimeout(() => this.initNotifications(), 5000);
    }
}
