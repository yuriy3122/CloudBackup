import { DxDataGridComponent } from "devextreme-angular";

export class DevExtremeHelper {

    static loadGridState(storageKey: string) {
        var stateJson = localStorage.getItem(storageKey);
        if (stateJson) {
            var state = JSON.parse(stateJson);
            state.searchText = "";
            state.selectedRowKeys = [];
            return state;
        }

        return {};
    }

    static saveGridState(storageKey: string, state: any) {
        localStorage.setItem(storageKey, JSON.stringify(state));
    }

    static scrollToRow(gridComponent: DxDataGridComponent, rowKey: any) {
        const grid = gridComponent.instance;
        const dataSource = grid.getDataSource();
        const keyExpr = dataSource.key();

        return Promise.resolve(dataSource.load())
            .then(items => {
                var key = Array.isArray(keyExpr) ? keyExpr[0] : keyExpr;
                // if the row is already on the page
                if (items.some((x: any) => x[key] === rowKey)) {
                    grid.deselectRows([rowKey]);
                    grid.selectRows([rowKey], true);
                    return Promise.resolve();
                }

                // get rowIndex from original dataSource by a key value
                var options: any = dataSource.loadOptions();
                options.skip = options.take = undefined;

                return Promise.resolve(dataSource.store().load(options))
                    .then((result: any) => {
                        let items: any[] = result.data || result;
                        let absoluteRowIndex = items.findIndex(x => x[key] === rowKey);

                        if (absoluteRowIndex >= 0) {
                            // check if we need to update page index first
                            var pageSize = grid.pageSize();
                            var pageIndex = Math.floor(absoluteRowIndex / pageSize);

                            if (pageIndex !== grid.pageIndex()) {
                                grid.pageIndex(pageIndex);
                            }

                            return grid.refresh();
                        }

                        return Promise.resolve();
                    });
            })
            .then(() => grid.selectRows([rowKey], true))
            .catch(err => console.log(err));
    }
}