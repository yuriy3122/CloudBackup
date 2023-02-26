export class ConfirmDialogContext {
    title = 'Confirm';
    text = 'Are you sure?';
    okText = 'Ok';
    cancelText = 'Ccancel';
    cancelVisible = true;
  canClose = false;

    callback: Function = (result: boolean) => null;
    constructor() {

    }
}
