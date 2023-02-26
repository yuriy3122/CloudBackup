export class BaseService {
  protected baseUrl = "/Core/";
  protected loginUrl = this.baseUrl + "Authentication/Bearer";

  protected configuredUrl = this.baseUrl + "Configuration/Configured";
  protected configurationUrl = this.baseUrl + "Configuration";

  protected backupsUrl = this.baseUrl + "Backup";
  protected backupJobsUrl = this.backupsUrl + "/Job";
  protected backupScriptsUrl = "Scripts";
  protected backupObjectDisks = this.backupsUrl + "/Disk";
  protected backupObjectInstances = this.backupsUrl + "/Instance";

  protected backupScheduleUrl = this.baseUrl + "Schedule";

  protected backupRegionsUrl = this.baseUrl + "Region";
  protected backupDisksUrl = this.baseUrl + "Disk";

  protected backupInstancesUrl = this.baseUrl + "Instance";

  protected administrationUrl = "/Administration/";
  protected profilesUrl = this.administrationUrl + "Profile";
  protected tenantsUrl = this.administrationUrl + "Tenant";
  protected rolesUrl = this.administrationUrl + "Role";
  protected azureAccountsUrl = this.administrationUrl + "AzureStorageAccount";
  //Permissions
  protected usersUrl = this.administrationUrl + "User";
  protected permissionsUrl = this.administrationUrl + "Permissions";

  protected cloudUrl = this.baseUrl + "Cloud";
  protected folderUrl = this.baseUrl + "Folder";
  protected restoreUrl = this.baseUrl + "Restore/Job";
  protected alertsUrl = this.baseUrl + "Alerts";

  //notifications
  protected notificationsUrl = this.baseUrl + "Notifications";
  protected notificationDeliveryConfigUrl = this.notificationsUrl + "/DeliveryConfigurations";

  protected extractData(res: any) {
    let json = res.json();
    return json || [];
  }

  //common method for file saving
  protected startDownloadFile(response: any, fileName: string) {
    var blob = new Blob([response], { type: response.type });
    const link = document.createElement('a');
    // Browsers that support HTML5 download attribute
    if (link.download !== undefined) {
      const url = URL.createObjectURL(blob);
      link.setAttribute('href', url);
      link.setAttribute('download', fileName);
      link.style.visibility = 'hidden';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    }
  }
}
