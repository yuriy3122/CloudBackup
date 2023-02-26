//inner
import { Component, Injectable, Input, OnInit, Pipe } from '@angular/core';

export class WizardStep {
    public Title: string;
    public Description: string;
    public SubDescription: string;
    public OnStepCreated: Function;
    public OnNextStep: Function;
    public IsValid: boolean;
    constructor(onStepCreated: Function, onNextStep: Function, title: string, description?: string, subDescription?: string) {
        this.OnStepCreated = onStepCreated;
        this.OnNextStep = onNextStep;
        this.IsValid = true;
        this.Description = description || "";
        this.SubDescription = subDescription || "";
        this.Title = title || "";
    }
}

export class WizardOptions {
    /** callbacks*/
    /** function that runs when wizard is creating*/
    public OnCreate!: Function;
    /** function that runs when we click Next button */
    public OnNextStep!: Function;
    /** function that runs when wizard is complete*/
    public OnComplete!: Function;
    /** some data info*/
    public Title!: string;
}
