import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { DevExtremeModule } from 'devextreme-angular';
import { SelectRowOnClickDirective } from "./services/common/SelectRowOnClickDirective";
import { AppComponent } from './app.component';
import { MainMenuComponent } from './components/menu/main-menu/main-menu.component';
import { AppRoutes } from './app.routes';
import { AuthenticationGuard } from './guards/AuthenticationGuard';
import { HttpService } from './services/http/HttpService';
import { SecondaryMenuComponent } from './components/menu/secondary-menu/secondary-menu.component';
import { BackupJobsComponent } from './components/backup/jobs/backup-jobs.component';
import { AddEditBackupJobWizard } from './dialogs/AddEditBackupJobWizard/AddEditBackupJobWizard';
import { ItemsListBlock } from './components/common/ItemsListBlock';
import { SelectInstancesWizard } from './dialogs/SelectInstancesWizard/SelectInstancesWizard';
import { SelectDailyIntervalsDialog } from './dialogs/SelectDailyIntervalsDialog/SelectDailyIntervalsDialog';
import { SelectDaysOfWeekDialog } from './dialogs/SelectDaysOfWeekDialog/SelectDaysOfWeekDialog';
import { SelectDisksWizard } from './dialogs/SelectDisksWizard/SelectDisksWizard';
import { JobScheduleEditorComponent } from './components/common/job-schedule-editor/job-schedule-editor.component';
import { SelectMonthsDialog } from './dialogs/SelectMonthsDialog/SelectMonthsDialog';
import { JobCostComponent } from './dialogs/AddEditBackupJobWizard/job-cost/job-cost.component';
import { DailyIntervalsSelectorComponent } from './components/common/daily-intevals-selector/daily-intervals-selector.component';
import { PermissionsGuard } from './guards/PermissionsGuard';
import { BackupService } from './services/backup/BackupService';
import { PermissionService } from './services/common/PermissionService';
import { AdministrationService } from './services/administration/AdministrationService';
import { NotificationService } from './services/common/NotificationService';
import { AuthService } from './services/auth/AuthService';
import { LoginComponent } from './components/login/login.component';
import { SkeletonDialog } from './dialogs/SkeletonDialog/skeletonDialog.component';
import { BackupBackupsComponent } from './components/backup/backups/backup-backups.component';
import { BackupsListComponent } from './components/backup/backups/backups-list.component';
import { ToolbarFilterComponent } from './components/common/toolbar-filter/toolbar-filter.component';
import { AdministrationProfilesComponent } from './components/administration/profiles/administration-profiles.component';
import { AddEditProfileDialog } from './dialogs/administration/AddEditProfileDialog/AddEditProfileDialog';
import { BackupRestoreComponent } from './components/restore/jobs/backup-restore.component';
import { AddRestoreJobWizard } from './dialogs/AddRestoreJobWizard/AddRestoreJobWizard';
import { ConfirmDialog } from './dialogs/ConfirmDialog/ConfirmDialog';
import { AdministrationTenantsComponent } from './components/administration/tenants/administration-tenants.component';
import { AddEditTenantDialog } from './dialogs/administration/AddEditTenantDialog/AddEditTenantDialog';
import { AdministrationUsersComponent } from './components/administration/users/administration-users.component';
import { AddEditUserDialog } from './dialogs/administration/AddEditUserDialog/AddEditUserDialog';
import { RestoreJobDiskOptionsDialog } from './dialogs/AddRestoreJobWizard/RestoreJobDiskOptionsDialog/RestoreJobDiskOptionsDialog';
import { RestoreJobInstanceOptionsDialog } from './dialogs/AddRestoreJobWizard/RestoreJobInstanceOptionsDialog/RestoreJobInstanceOptionsDialog';
import { AlertsComponent } from './components/common/alerts/alerts.component';
import { AlertsDialog } from './dialogs/AlertsDialog/AlertsDialog';
import { AlertService } from './services/alerts/AlertService';
import { WizardDialog } from './dialogs/Wizard/WizardDialog';
import { WizardStepComponent } from './dialogs/Wizard/WizardStep.component';
import { SettingsNotificationsComponent } from './components/settings/notifications/settings-notifications.component';
import { AddNotificationWizard } from './dialogs/AddEditNotificationWizard/AddNotificationWizard';
import { InfrastructureInstancesComponent } from './components/infrastructure/instance/infrastructure-instances.component';
import { RegistrationWizard } from './dialogs/RegistrationWizard/RegistrationWizard';

@NgModule({
  declarations: [
    ItemsListBlock,
    JobScheduleEditorComponent,
    JobCostComponent,
    SelectMonthsDialog,
    SelectInstancesWizard,
    SelectDisksWizard,
    SelectDailyIntervalsDialog,
    SelectDaysOfWeekDialog,
    AddRestoreJobWizard,
    AddEditProfileDialog,
    AddEditTenantDialog,
    AddEditUserDialog,
    RestoreJobInstanceOptionsDialog,
    RestoreJobDiskOptionsDialog,
    DailyIntervalsSelectorComponent,
    AddEditBackupJobWizard,
    AddNotificationWizard,
    AppComponent,
    MainMenuComponent,
    SecondaryMenuComponent,
    ConfirmDialog,
    BackupJobsComponent,
    LoginComponent,
    SkeletonDialog,
    BackupBackupsComponent,
    BackupsListComponent,
    ToolbarFilterComponent,
    AdministrationProfilesComponent,
    AdministrationTenantsComponent,
    AdministrationUsersComponent,
    BackupRestoreComponent,
    SelectRowOnClickDirective,
    AlertsComponent,
    AlertsDialog,
    WizardDialog,
    WizardStepComponent,
    SettingsNotificationsComponent,
    InfrastructureInstancesComponent,
    RegistrationWizard
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    DevExtremeModule,
    RouterModule.forRoot(AppRoutes.root),
    RouterModule.forChild([
      ...AppRoutes.leftMenu,
      ...AppRoutes.BackupMenu,
      ...AppRoutes.RestoreMenu,
      ...AppRoutes.AdministrationMenu,
      ...AppRoutes.SettingsMenu,
      ...AppRoutes.InfrastructureMenu
    ])
  ],
  providers: [
    AuthenticationGuard,
    PermissionsGuard,
    PermissionService,
    BackupService,
    NotificationService,
    PermissionService,
    BackupService,
    AuthService,
    AdministrationService,
    AlertService,
    HttpService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
