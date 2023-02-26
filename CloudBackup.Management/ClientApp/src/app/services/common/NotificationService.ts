import { Injectable, Input, EventEmitter, Output} from '@angular/core';
import notify from 'devextreme/ui/notify';
import { Message, MessageList } from 'src/app/classes/Message';
import { ConfirmDialogContext } from 'src/app/dialogs/ConfirmDialog/ConfirmDialogContext';
import { environment } from 'src/environments/environment';
import { HttpService } from '../http/HttpService';

@Injectable({
    providedIn: 'root'
})
export class NotificationService {
    //show loading spinner?
    private loading: boolean; 
    //show local loading spinner?
    private loadingChild: boolean;
    //all messages
    private messages: MessageList;
         
    @Output()
    public loadingObserver = new EventEmitter<boolean>();

    @Output()
    public loadingChildObserver = new EventEmitter<boolean>();

    @Output()
    public messageObserver = new EventEmitter<MessageList>();

    @Output()
    public confirmDialogObserver = new EventEmitter<ConfirmDialogContext>();

    constructor(private httpService: HttpService) {
        this.loading = false;
        this.loadingChild = false;
        this.messages = new MessageList();
        
        //console.clear();
        console.log('Notification service created');
    }
    
    @Input()
    public setLoading(value: boolean, child?: boolean) {
        if (child == true) {
            this.loadingChild = value;
            this.loadingChildObserver.emit(this.loadingChild);
        }
        else {
            this.loading = value;
            this.loadingObserver.emit(this.loading);
        }
        return this.loadingObserver;
    }

    @Input()
    public addMessage(message?: Message) {
        if (message != null) {
            this.messages.Add(message);
            this.messageObserver.emit(this.messages);
        }
        return this.messageObserver;
    }

    @Input()
    public removeMessage(id?: string) {
        if (id != null) {
            this.messages.Remove(id);
            this.messageObserver.emit(this.messages);
        }
        return this.messageObserver;
    }

    @Input()
    public clearMessages() {
        this.messages.Clear();
        this.messageObserver.emit(this.messages);
        return this.messageObserver;
    }

    public getLoadingState() {
        return this.loadingObserver;
    }

    public getLoadingChildState() {
        return this.loadingChildObserver;
    }

    public getMessageObserver() {
        return this.messageObserver;
    }
    
    public showInfoNotification(message: string, delay?: number): void {
        this.showNotification(message, 'info', delay || 6000);
    }
    public showWarningNotification(message: string, delay?: number): void {
        this.showNotification(message, 'warning', delay || 6000);
    }
    public showErrorNotification(message: string, delay?: number): void {
        this.showNotification(message, 'error', delay || 6000);
    }
    public showSuccessNotification(message: string, delay?: number): void {
        this.showNotification(message, 'success', delay || 6000);
    }
    private showNotification(message: string, messageType: string, delay: number): void {
        notify(message, messageType, delay);
    }


    public getConfirmDialogObserver() {
        return this.confirmDialogObserver;
    }

    public alert(callback: (r: boolean) => void, message: string, title = 'Warning', okText = 'OK') {
        this.showDialog(callback, message, title, okText, '', true);
    }

    public confirm(callback: (r: boolean) => void, message: string, title = 'Confirmation', okText = 'OK', cancelText = 'Cancel') {
        this.showDialog(callback, message, title, okText, cancelText, false);
    }

    public confirmYesNo(callback: (r: boolean) => void, message: string, title = 'Confirmation', okText = 'Yes', cancelText = 'No') {
        this.showDialog(callback, message, title, okText, cancelText, false);
    }

    private showDialog(callback: (r: boolean) => void, message: string, title: string, okText: string, cancelText: string, canClose: boolean) {
        var context = new ConfirmDialogContext();
        
        context.title = title;
        context.text = message;
        context.okText = okText;
        context.cancelText = cancelText;
        context.cancelVisible = cancelText != null;
        context.canClose = canClose;
        context.callback = callback;

        this.confirmDialogObserver.emit(context);
    }

    getSocket(path: string) {
        //NB: proxy config works awfull when I set websocket config
        //easiest way to use websockets is to set port manually or via config
        const port = environment.port; //location.port ? location.port : "443";
        const url = `${this.httpService.socketProtocol}//${window.location.hostname}${port ? ':' + port : ''}/${path}`;
        const socket = new WebSocket(url);
        socket.binaryType = "blob";
        return socket;
    }
}