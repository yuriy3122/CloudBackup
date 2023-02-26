export class CloudFolder {
    public id?: string;
    public cloudId?: string;
    public createdAt?: string;
    public name?: string;
    public description?: string;
    public labels?: string;
    public status?: string;

    constructor(id?: string) {
        this.id = id != null ? id : '';
    }

    static Copy(obj: CloudFolder): CloudFolder {
        let copy = Object.assign({}, obj);
        return copy;
    }
}
