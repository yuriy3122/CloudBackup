export class Instance {
    public Id: string;
    public Name: string;

    public SelectedProfile: string;
    public SelectedRegion: string;

    constructor(id?: string) {
        this.Id = id != null ? id : "";
        this.Name = '';
    }

    static Copy(obj: any): Instance {
        let copy = new Instance(obj.Id || obj.id || "");
        copy.Name = obj.Name || obj.name || "";
        copy.SelectedProfile = obj.SelectedProfile || obj.selectedProfile;
        copy.SelectedRegion = obj.SelectedRegion || obj.selectedRegion || "";

        return copy;
    }
}