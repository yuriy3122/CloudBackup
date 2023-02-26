export class DataGridSettings {
  public readonly allowedPageSizes = [5, 10,20];

  public readonly displayModes = [{ text: "Display Mode 'full'", value: 'full' }, { text: "Display Mode 'compact'", value: 'compact' }];

  public displayMode = 'full';

  public showPageSizeSelector = true;

  public showInfo = false;

  public showNavButtons = true;
  public backclass = { class: 'button secondary back' };
  public nextclass = { class: 'button secondary next' };
  public finishclass = { class: 'button primary finish' };
  public cancelclass = { class: 'button secondary' };
}

