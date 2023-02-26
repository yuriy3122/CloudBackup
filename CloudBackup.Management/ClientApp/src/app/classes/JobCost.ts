import { Formatters } from "../helpers/Formatters";
import { JobObjectType } from "./Enums";

export class JobCost {
    cost = 0;
    currency = "₽";
    objectCosts!: JobObjectCost[];
    dailyDataChangeRatio!: number;
    usedDiskSpaceRatio!: number;
    compressionRatio!: number;

    constructor(rawObject?: any) {
        if (rawObject != null) {
            Object.assign(this, rawObject);

            if (this.objectCosts != null)
                this.objectCosts = this.objectCosts.map(x => new JobObjectCost(x));
        }
    }
}

export class JobObjectCost {
    jobObjectType: JobObjectType = 0;
    details!: JobCostDetails[];

    get jobObjectTypeName() {
        return Formatters.getJobObjectTypeShortPlural(this.jobObjectType);
    }

    constructor(rawObject?: any) {
        if (rawObject != null) {
            Object.assign(this, rawObject);

            if (this.details != null)
                this.details = this.details.map(x => new JobCostDetails(x));
        }
    }
}

export class JobCostDetails {
    costDescription!: string;
    cost!: number;
    currency!: string;

    constructor(rawObject?: any) {
        if (rawObject != null) {
            Object.assign(this, rawObject);
        }
    }
}
