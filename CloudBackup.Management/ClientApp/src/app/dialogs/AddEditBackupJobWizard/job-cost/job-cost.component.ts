import { Component, Input, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { BackupJob } from "../../../classes/BackupJob";
import { JobCost } from "../../../classes/JobCost";
import { BackupService } from "../../../services/backup/BackupService";
import { interval, Observable, timer } from "rxjs";
import { takeUntil } from 'rxjs/operators';

@Component({
    selector: 'job-cost',
    templateUrl: './job-cost.template.html'
})
export class JobCostComponent {
    @Input()
    backupJob!: BackupJob;

    @Output()
    jobCost!: JobCost | null;
    jobCostChange = new EventEmitter<JobCost>();

    jobCostLoading = false;

    constructor(private backupService: BackupService) {
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.backupJob) {
            this.updateCost();
        }
    }

    private updateCost() {
        const job = this.backupJob;

        if (job == null) {
            this.jobCost = null;
            return;
        } else if (job.JobObjects.length === 0) {
            this.jobCost = new JobCost();
            return;
        }

        try {
            const promise = this.backupService.getJobCost(job)
                .then(cost => {
                    this.jobCost = cost;
                    this.jobCostLoading = false;
                })
                .catch(e => {
                    this.jobCost = null;
                    this.jobCostLoading = false;
                });

            // timer(300)
            //     .takeUntil(Observable.from(promise))
            //     .subscribe(() => this.jobCostLoading = true);

            const source = interval(300);
            const t = timer(300);
            const task = source.pipe(takeUntil(t));
            task.subscribe(() => this.jobCostLoading = true);
        } catch (e) {
            console.error("Error while updating job cost" + (e ? `: ${e}` : ""));
            this.jobCost = null;
            this.jobCostLoading = false;
        }
    }
}