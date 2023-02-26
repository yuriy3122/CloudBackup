//inner
import { Component, Injectable, Input, Output, OnInit, EventEmitter, DoCheck, ViewChild } from '@angular/core';

@Component({
    selector: 'items-list-block',
    template: '<div style="overflow:auto; height:200px; float:left"><p *ngFor="let item of items">{{ item.value }}</p></div>'
})

@Injectable()
export class ItemsListBlock {
    @Input()
    items: { value: string }[] = [];
}