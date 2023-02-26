export const defaultPageSize: number = 15;

export const diskColumns = [
    { caption: "Name", dataField: "name" },
  { caption: "Description", dataField: "description" },
    { caption: "Disk Id", dataField: "id" },
    { caption: "Status", dataField: "status" },
    { caption: "Type", dataField: "typeId" },
    { caption: "Capacity", dataField: "size" }, //, calculateSortValue: "storageSize" 
    { caption: "Zone ID", dataField: "zoneId" }
];

export const instanceColumns = [
    { caption: "Name", dataField: "name" },
    { caption: "Instance Id", dataField: "id" },
  { caption: "Description", dataField: "description" },
  { caption: "Zone ID", dataField: "zoneId" },
  { caption: "Status", dataField: "status" }
];
