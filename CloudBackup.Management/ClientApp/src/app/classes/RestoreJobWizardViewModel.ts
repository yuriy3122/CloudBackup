import { RestoreJobItemViewModel } from './RestoreJobItemViewModel';

export class RestoreJobWizardViewModel {
    public SelectedTenantId: number;
    public SelectedBackupIds: Array<number>;
    public SelectedItems: Array<RestoreJobItemViewModel>;

    constructor(selectedTenantId: number) {
        this.SelectedTenantId = selectedTenantId;
        this.SelectedBackupIds = [];
        this.SelectedItems = [];
    }

    static Copy(obj: any): RestoreJobWizardViewModel {
        let copy = new RestoreJobWizardViewModel(obj.SelectedTenantId || obj.selectedTenantId);

        if (Array.isArray(obj.SelectedBackupIds))
            copy.SelectedBackupIds = obj.SelectedBackupIds;
        else if (Array.isArray(obj.selectedBackupIds))
            copy.SelectedBackupIds = obj.selectedBackupIds;

        if (Array.isArray(obj.SelectedItems))
            copy.SelectedItems = obj.SelectedItems;
        else if (Array.isArray(obj.selectedItems))
            copy.SelectedItems = obj.selectedItems;


        return copy;
    }
}