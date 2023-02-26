import { Route, Routes } from "@angular/router";
import { AuthenticationGuard } from "./guards/AuthenticationGuard";
import { BackupJobsComponent } from "./components/backup/jobs/backup-jobs.component";
import { CustomPermissions, UserPermissions } from "./classes/UserPermissions";
import { PermissionsGuard } from "./guards/PermissionsGuard";
import { LoginComponent } from "./components/login/login.component";
import { BackupBackupsComponent } from "./components/backup/backups/backup-backups.component";
import { AdministrationProfilesComponent } from "./components/administration/profiles/administration-profiles.component";
import { BackupRestoreComponent } from "./components/restore/jobs/backup-restore.component";
import { AdministrationTenantsComponent } from "./components/administration/tenants/administration-tenants.component";
import { AdministrationUsersComponent } from "./components/administration/users/administration-users.component";
import { SettingsNotificationsComponent } from "./components/settings/notifications/settings-notifications.component";
import { InfrastructureInstancesComponent } from "./components/infrastructure/instance/infrastructure-instances.component";
import { RegistrationWizard } from "./dialogs/RegistrationWizard/RegistrationWizard";

export class AppRoutes {
    //#region backup routes

    static BackupJobsRoute: Route = {
        path: BackupJobsComponent.path,
        data: { title: BackupJobsComponent.title, class: "backup", active: false, hasPermissions: BackupJobsComponent.checkPermissions },
        component: BackupJobsComponent,
        canActivate: [AuthenticationGuard, PermissionsGuard] //, PermissionsGuard
    };

    static BackupBackupsRoute: Route = {
        path: BackupBackupsComponent.path,
        data: { title: BackupBackupsComponent.title, class: "backup", active: false, hasPermissions: BackupJobsComponent.checkPermissions },
        component: BackupBackupsComponent,
        canActivate: [AuthenticationGuard, PermissionsGuard] //, PermissionsGuard
    };

    static BackupMenu: Routes = [
        AppRoutes.BackupJobsRoute,
        AppRoutes.BackupBackupsRoute
    ];

    static BackupRoute: Route = {
        path: "Backup",
        data: {
          title: "Backup",
            class: "backup",
            children: AppRoutes.BackupMenu,
            active: false,
            hasPermissions: (p: UserPermissions) => (p.BackupRights & CustomPermissions.Read) === CustomPermissions.Read
        },
        redirectTo: AppRoutes.BackupJobsRoute.path,
        pathMatch: 'full'
    };

    //#endregion

    //#region Administration
    static ProfilesRoute: Route = {
        path: AdministrationProfilesComponent.path,
        data: {
            title: AdministrationProfilesComponent.title,
            class: "profile",
            children: [],
            active: false,
            hasPermissions: AdministrationProfilesComponent.checkPermissions
        },
        component: AdministrationProfilesComponent,
        canActivate: [AuthenticationGuard, PermissionsGuard]
    };

    static TenantsRoute: Route = {
        path: AdministrationTenantsComponent.path,
        data: {
            title: AdministrationTenantsComponent.title,
            class: "tenant",
            children: [],
            active: false,
            hasPermissions: AdministrationTenantsComponent.checkPermissions
        },
        component: AdministrationTenantsComponent,
        canActivate: [AuthenticationGuard, PermissionsGuard]
    };

    static UsersRoute: Route = {
        path: AdministrationUsersComponent.path,
        data: {
            title: AdministrationUsersComponent.title,
            class: "user",
            children: [],
            active: false,
            hasPermissions: AdministrationUsersComponent.checkPermissions
        },
        component: AdministrationUsersComponent,
        canActivate: [AuthenticationGuard, PermissionsGuard]
    };

    static AdministrationMenu: Routes = [
        AppRoutes.ProfilesRoute,
        AppRoutes.TenantsRoute,
        AppRoutes.UsersRoute
    ];

    //user administration menu route
    static AdministrationRoute: Route = {
        path: AppRoutes.ProfilesRoute.path,
        data: {
            title: "User Administration",
            class: "administration",
            children: AppRoutes.AdministrationMenu,
            active: false,
            hasPermissions: (p: UserPermissions) => p.IsGlobalAdmin || p.IsUserAdmin
        },
        redirectTo: AppRoutes.ProfilesRoute.path,
        pathMatch: 'full',
    };
    //#endregion

    //#region Restore
    static RestoreJobRoute: Route = {
        path: BackupRestoreComponent.path,
        data: { title: BackupRestoreComponent.title, class: "restore", children: [], active: false, hasPermissions: BackupRestoreComponent.checkPermissions },
        component: BackupRestoreComponent,
        canActivate: [AuthenticationGuard, PermissionsGuard]
    };

    static RestoreMenu: Routes = [
        AppRoutes.RestoreJobRoute
    ];

    static RestoreRoute: Route = {
        path: "Restore",
        data: {
          title: "Restore",
            class: "restore",
            children: AppRoutes.RestoreMenu,
            active: false,
            hasPermissions: (p: UserPermissions) => (p.RestoreRights & CustomPermissions.Read) === CustomPermissions.Read
        },
        redirectTo: AppRoutes.RestoreJobRoute.path,
        pathMatch: 'full'
    };
    //#endregion

    static NotificationsRoute: Route = {
        path: SettingsNotificationsComponent.path,
        data: { title: SettingsNotificationsComponent.title, class: "notification", children: [], active: false },
        component: SettingsNotificationsComponent,
        canActivate: [AuthenticationGuard, PermissionsGuard]
    };

    static SettingsMenu: Routes = [
        AppRoutes.NotificationsRoute
    ];

    static SettingsRoute: Route = {
        path: "Settings",
        data: {
            title: "Settings",
            class: "settings",
            children: AppRoutes.SettingsMenu,
            active: false,
            hasPermissions: (p: UserPermissions) => p.IsGlobalAdmin || p.IsUserAdmin
        },
        redirectTo: AppRoutes.NotificationsRoute.path,
        pathMatch: 'full',
    };

    //#region Infrastructure

    // Instances

    static BackupInstanceRoute: Route = {
        path: InfrastructureInstancesComponent.path,
        data: { title: InfrastructureInstancesComponent.title, class: "instance", active: false },
        component: InfrastructureInstancesComponent,
        canActivate: [AuthenticationGuard, PermissionsGuard]
    };

    static InfrastructureMenu: Routes = [
        AppRoutes.BackupInstanceRoute
    ];

    //infrastructure menu route
    static InfrastructureRoute: Route = {
        path: "Infrastructure",
        data: {
          title: "Infrastructure", class: "infrastructure", children: AppRoutes.InfrastructureMenu, active: false
        },
        redirectTo: AppRoutes.BackupInstanceRoute.path,
        pathMatch: 'full',
    };
    //#endregion


    //left menu routes
    static leftMenu: Routes = [
        AppRoutes.InfrastructureRoute,
        AppRoutes.BackupRoute,
        AppRoutes.RestoreRoute,
        AppRoutes.AdministrationRoute,
        AppRoutes.SettingsRoute
    ];

    //root
    static root: Routes = [
        {
            path: '',
            redirectTo: AppRoutes.BackupJobsRoute.path,
            pathMatch: 'full',
            //canActivate: [AuthenticationGuard, PermissionsGuard]
        },
        {
            path: 'Login',
            data: { title: "Login" },
            component: LoginComponent
        },
        {
            path: 'Registration',
            data: { title: "Registration" },
            component: RegistrationWizard
        }
    ];
}
