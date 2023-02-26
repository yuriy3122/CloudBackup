using Microsoft.EntityFrameworkCore;
using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class BackupContext : DbContext
    {
        public BackupContext(DbContextOptions<BackupContext> options)
            : base(options)
        {}

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Tenant> Tenants { get; set; } = null!;
        public DbSet<Schedule> Schedules { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<Profile> Profiles { get; set; } = null!;
        public DbSet<Job> Jobs { get; set; } = null!;
        public DbSet<JobObject> JobObjects { get; set; } = null!;
        public DbSet<Backup> Backups { get; set; } = null!;
        public DbSet<BackupObject> BackupObjects { get; set; } = null!;
        public DbSet<BackupLog> BackupLogs { get; set; } = null!;
        public DbSet<RestoreJob> RestoreJob { get; set; } = null!;
        public DbSet<RestoreJobObject> RestoreJobObjects { get; set; } = null!;
        public DbSet<Log> Log { get; set; } = null!;
        public DbSet<TenantProfile> TenantProfiles { get; set; } = null!;
        public DbSet<JobConfiguration> JobConfigurations { get; set; } = null!;
        public DbSet<Alert> Alerts { get; set; } = null!;
        public DbSet<UserAlert> UserAlerts { get; set; } = null!;
        public DbSet<NotificationConfiguration> NotificationConfigurations { get; set; } = null!;
        public DbSet<NotificationDeliveryConfiguration> NotificationDeliveryConfigurations { get; set; } = null!;
        public DbSet<Configuration> Configurations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            AddTypeConfigurations(modelBuilder);
        }

        private static void AddTypeConfigurations(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<EntityBase>();

            modelBuilder.Entity<Tenant>().Property(p => p.Name).IsRequired();
            modelBuilder.Entity<Tenant>()
                .HasMany(x => x.Users)
                .WithOne(x => x.Tenant)
                .HasForeignKey(x => x.TenantId);

            modelBuilder.Entity<User>().Property(p => p.Name).IsRequired();
            modelBuilder.Entity<User>().Property(p => p.IsEnabled).IsRequired();
            modelBuilder.Entity<User>().Property(p => p.Login).IsRequired().HasMaxLength(128);
            modelBuilder.Entity<User>().HasIndex(p => p.Login).IsUnique();
            modelBuilder.Entity<User>()
                .HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId);

            modelBuilder.Entity<UserRole>().HasKey(t => new { t.RoleId, t.UserId });
            modelBuilder.Entity<UserRole>().Ignore(p => p.Id);

            modelBuilder.Entity<UserRole>()
                .HasOne(pt => pt.User)
                .WithMany(p => p.UserRoles)
                .HasForeignKey(pt => pt.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(pt => pt.Role)
                .WithMany(t => t.UserRoles)
                .HasForeignKey(pt => pt.RoleId);

            modelBuilder.Entity<TenantProfile>().HasKey(t => new { t.TenantId, t.ProfileId });
            modelBuilder.Entity<TenantProfile>().Ignore(p => p.Id);

            modelBuilder.Entity<TenantProfile>()
                .HasOne(pt => pt.Tenant)
                .WithMany(p => p.TenantProfiles)
                .HasForeignKey(pt => pt.TenantId);

            modelBuilder.Entity<TenantProfile>()
                .HasOne(pt => pt.Profile)
                .WithMany(t => t.TenantProfiles)
                .HasForeignKey(pt => pt.ProfileId);

            modelBuilder.Entity<Profile>().Property(p => p.Name).IsRequired();
            modelBuilder.Entity<Profile>()
                .HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Schedule>().Property(p => p.StartupType).IsRequired();

            // Jobs
            modelBuilder.Entity<Job>().Property(p => p.Type).IsRequired();
            modelBuilder.Entity<Job>().Property(p => p.Status).IsRequired();
            modelBuilder.Entity<Job>().Property(p => p.Name).IsRequired();
            modelBuilder.Entity<Job>()
                .HasOne(x => x.Configuration)
                .WithOne(x => x.Job)
                .HasForeignKey<JobConfiguration>(x => x.JobId);
            modelBuilder.Entity<Job>()
                .HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Job>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<JobConfiguration>().Property(p => p.JobId).IsRequired();
            modelBuilder.Entity<JobObject>().Property(p => p.Region).IsRequired();
            modelBuilder.Entity<JobObject>().Property(p => p.Type).IsRequired();
            modelBuilder.Entity<JobObject>().Property(p => p.ObjectId).IsRequired();
            modelBuilder.Entity<JobObject>()
                .HasOne(x => x.Profile)
                .WithMany()
                .HasForeignKey(x => x.ProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // Backup
            modelBuilder.Entity<Backup>().HasMany(x => x.BackupLogs).WithOne().HasForeignKey(x => x.BackupId);
            modelBuilder.Entity<BackupObject>().Property(p => p.SourceObjectId).IsRequired();
            modelBuilder.Entity<BackupObject>().Property(p => p.Region).IsRequired();
            modelBuilder.Entity<BackupObject>()
                .HasOne(x => x.Profile)
                .WithMany()
                .HasForeignKey(x => x.ProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RestoreJob>()
                .HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RestoreJob>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<RestoreJobObject>().Property(p => p.Status).IsRequired();
            modelBuilder.Entity<RestoreJobObject>()
                .HasOne(x => x.BackupObject)
                .WithMany()
                .HasForeignKey(x => x.BackupObjectId)
                .OnDelete(DeleteBehavior.SetNull);

            // Log
            modelBuilder.Entity<Log>().Property(p => p.EventDate).IsRequired();
            modelBuilder.Entity<Log>().Property(p => p.ObjectType).IsRequired().HasMaxLength(256);
            modelBuilder.Entity<Log>().Property(p => p.ObjectId).IsRequired();
            modelBuilder.Entity<Log>().HasIndex(p => new {p.ObjectType, p.ObjectId});
        }
    }
}