﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using CloudBackup.Repositories;

#nullable disable

namespace CloudBackup.Repository.Migrations
{
    [DbContext(typeof(BackupContext))]
    [Migration("20220504084039_JobObject-CloudId-remove")]
    partial class JobObjectCloudIdremove
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("CloudBackup.Model.Backup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime?>("FinishedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("IsArchive")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsPermanent")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("JobConfiguration")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("JobId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<DateTime?>("StartedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("Status")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("JobId");

                    b.ToTable("Backups");
                });

            modelBuilder.Entity("CloudBackup.Model.BackupLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("BackupId")
                        .HasColumnType("int");

                    b.Property<DateTime>("EventDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<byte>("Severity")
                        .HasColumnType("tinyint unsigned");

                    b.HasKey("Id");

                    b.HasIndex("BackupId");

                    b.ToTable("BackupLogs");
                });

            modelBuilder.Entity("CloudBackup.Model.BackupObject", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("BackupId")
                        .HasColumnType("int");

                    b.Property<string>("BackupParams")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("DestObjectId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("FinishedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("ParentId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("ProfileId")
                        .HasColumnType("int");

                    b.Property<string>("Region")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<string>("SourceObjectId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("StartedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("BackupId");

                    b.HasIndex("ProfileId");

                    b.ToTable("BackupObjects");
                });

            modelBuilder.Entity("CloudBackup.Model.Job", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("LastRunAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("NextRunAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("ObjectCount")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("int");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<bool>("RunDelayed")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("RunNow")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("ScheduleId")
                        .HasColumnType("int");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<int?>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ScheduleId");

                    b.HasIndex("TenantId");

                    b.HasIndex("UserId");

                    b.ToTable("Jobs");
                });

            modelBuilder.Entity("CloudBackup.Model.JobConfiguration", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Configuration")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("JobId")
                        .HasColumnType("int");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.HasKey("Id");

                    b.HasIndex("JobId")
                        .IsUnique();

                    b.ToTable("JobConfigurations");
                });

            modelBuilder.Entity("CloudBackup.Model.JobObject", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("CustomData")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("FolderId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("JobId")
                        .HasColumnType("int");

                    b.Property<string>("ObjectId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("ParentId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("ProfileId")
                        .HasColumnType("int");

                    b.Property<string>("Region")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("JobId");

                    b.HasIndex("ProfileId");

                    b.ToTable("JobObjects");
                });

            modelBuilder.Entity("CloudBackup.Model.Log", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("EventDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("MessageText")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("ObjectId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("ObjectType")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<byte>("Severity")
                        .HasColumnType("tinyint unsigned");

                    b.Property<int?>("UserId")
                        .HasColumnType("int");

                    b.Property<string>("XmlData")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("ObjectType", "ObjectId");

                    b.ToTable("Log");
                });

            modelBuilder.Entity("CloudBackup.Model.Profile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("AuthenticationType")
                        .HasColumnType("int");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Description")
                        .HasColumnType("longtext");

                    b.Property<bool>("IsSystem")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("KeyId")
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("OwnerUserId")
                        .HasColumnType("int");

                    b.Property<string>("PrivateKey")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<string>("ServiceAccountId")
                        .HasColumnType("longtext");

                    b.Property<byte>("State")
                        .HasColumnType("tinyint unsigned");

                    b.HasKey("Id");

                    b.HasIndex("OwnerUserId");

                    b.ToTable("Profiles");
                });

            modelBuilder.Entity("CloudBackup.Model.RestoreJob", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime?>("FinishedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("RestoreMode")
                        .HasColumnType("int");

                    b.Property<int?>("Result")
                        .HasColumnType("int");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<int?>("ScheduleId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("StartedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.Property<int?>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ScheduleId");

                    b.HasIndex("TenantId");

                    b.HasIndex("UserId");

                    b.ToTable("RestoreJob");
                });

            modelBuilder.Entity("CloudBackup.Model.RestoreJobObject", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("BackupObjectId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("FinishedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("GroupGuid")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("NewObjectId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("ParentId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("RestoreJobId")
                        .HasColumnType("int");

                    b.Property<string>("RestoreParams")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<DateTime?>("StartedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("BackupObjectId");

                    b.HasIndex("RestoreJobId");

                    b.ToTable("RestoreJobObjects");
                });

            modelBuilder.Entity("CloudBackup.Model.Role", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<byte>("BackupRights")
                        .HasColumnType("tinyint unsigned");

                    b.Property<byte>("InfrastructureRights")
                        .HasColumnType("tinyint unsigned");

                    b.Property<bool>("IsGlobalAdmin")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsUserAdmin")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<byte>("ProfileRights")
                        .HasColumnType("tinyint unsigned");

                    b.Property<byte>("RestoreRights")
                        .HasColumnType("tinyint unsigned");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<byte>("SecurityRights")
                        .HasColumnType("tinyint unsigned");

                    b.Property<byte>("TenantRights")
                        .HasColumnType("tinyint unsigned");

                    b.Property<byte>("UserRights")
                        .HasColumnType("tinyint unsigned");

                    b.HasKey("Id");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("CloudBackup.Model.Schedule", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("JobCount")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("OccurType")
                        .HasColumnType("int");

                    b.Property<string>("Params")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<int>("StartupType")
                        .HasColumnType("int");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("TenantId");

                    b.ToTable("Schedules");
                });

            modelBuilder.Entity("CloudBackup.Model.Tenant", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("IsSystem")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("Isolated")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.HasKey("Id");

                    b.ToTable("Tenants");
                });

            modelBuilder.Entity("CloudBackup.Model.TenantProfile", b =>
                {
                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.Property<int>("ProfileId")
                        .HasColumnType("int");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.HasKey("TenantId", "ProfileId");

                    b.HasIndex("ProfileId");

                    b.ToTable("TenantProfiles");
                });

            modelBuilder.Entity("CloudBackup.Model.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Login")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("OwnerUserId")
                        .HasColumnType("int");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("PasswordSalt")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.Property<long>("UtcOffsetTicks")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("Login")
                        .IsUnique();

                    b.HasIndex("OwnerUserId");

                    b.HasIndex("TenantId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("CloudBackup.Model.UserRole", b =>
                {
                    b.Property<int>("RoleId")
                        .HasColumnType("int");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.HasKey("RoleId", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("UserRoles");
                });

            modelBuilder.Entity("CloudBackup.Model.Backup", b =>
                {
                    b.HasOne("CloudBackup.Model.Job", "Job")
                        .WithMany()
                        .HasForeignKey("JobId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Job");
                });

            modelBuilder.Entity("CloudBackup.Model.BackupLog", b =>
                {
                    b.HasOne("CloudBackup.Model.Backup", null)
                        .WithMany("BackupLogs")
                        .HasForeignKey("BackupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("CloudBackup.Model.BackupObject", b =>
                {
                    b.HasOne("CloudBackup.Model.Backup", "Backup")
                        .WithMany("BackupObjects")
                        .HasForeignKey("BackupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CloudBackup.Model.Profile", "Profile")
                        .WithMany()
                        .HasForeignKey("ProfileId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Backup");

                    b.Navigation("Profile");
                });

            modelBuilder.Entity("CloudBackup.Model.Job", b =>
                {
                    b.HasOne("CloudBackup.Model.Schedule", "Schedule")
                        .WithMany()
                        .HasForeignKey("ScheduleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CloudBackup.Model.Tenant", "Tenant")
                        .WithMany()
                        .HasForeignKey("TenantId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("CloudBackup.Model.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Schedule");

                    b.Navigation("Tenant");

                    b.Navigation("User");
                });

            modelBuilder.Entity("CloudBackup.Model.JobConfiguration", b =>
                {
                    b.HasOne("CloudBackup.Model.Job", "Job")
                        .WithOne("Configuration")
                        .HasForeignKey("CloudBackup.Model.JobConfiguration", "JobId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Job");
                });

            modelBuilder.Entity("CloudBackup.Model.JobObject", b =>
                {
                    b.HasOne("CloudBackup.Model.Job", "Job")
                        .WithMany("JobObjects")
                        .HasForeignKey("JobId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CloudBackup.Model.Profile", "Profile")
                        .WithMany()
                        .HasForeignKey("ProfileId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Job");

                    b.Navigation("Profile");
                });

            modelBuilder.Entity("CloudBackup.Model.Profile", b =>
                {
                    b.HasOne("CloudBackup.Model.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerUserId")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("CloudBackup.Model.RestoreJob", b =>
                {
                    b.HasOne("CloudBackup.Model.Schedule", "Schedule")
                        .WithMany()
                        .HasForeignKey("ScheduleId");

                    b.HasOne("CloudBackup.Model.Tenant", "Tenant")
                        .WithMany()
                        .HasForeignKey("TenantId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("CloudBackup.Model.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Schedule");

                    b.Navigation("Tenant");

                    b.Navigation("User");
                });

            modelBuilder.Entity("CloudBackup.Model.RestoreJobObject", b =>
                {
                    b.HasOne("CloudBackup.Model.BackupObject", "BackupObject")
                        .WithMany()
                        .HasForeignKey("BackupObjectId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("CloudBackup.Model.RestoreJob", "RestoreJob")
                        .WithMany("RestoreJobObjects")
                        .HasForeignKey("RestoreJobId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BackupObject");

                    b.Navigation("RestoreJob");
                });

            modelBuilder.Entity("CloudBackup.Model.Schedule", b =>
                {
                    b.HasOne("CloudBackup.Model.Tenant", "Tenant")
                        .WithMany()
                        .HasForeignKey("TenantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Tenant");
                });

            modelBuilder.Entity("CloudBackup.Model.TenantProfile", b =>
                {
                    b.HasOne("CloudBackup.Model.Profile", "Profile")
                        .WithMany("TenantProfiles")
                        .HasForeignKey("ProfileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CloudBackup.Model.Tenant", "Tenant")
                        .WithMany("TenantProfiles")
                        .HasForeignKey("TenantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Profile");

                    b.Navigation("Tenant");
                });

            modelBuilder.Entity("CloudBackup.Model.User", b =>
                {
                    b.HasOne("CloudBackup.Model.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerUserId");

                    b.HasOne("CloudBackup.Model.Tenant", "Tenant")
                        .WithMany("Users")
                        .HasForeignKey("TenantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Owner");

                    b.Navigation("Tenant");
                });

            modelBuilder.Entity("CloudBackup.Model.UserRole", b =>
                {
                    b.HasOne("CloudBackup.Model.Role", "Role")
                        .WithMany("UserRoles")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CloudBackup.Model.User", "User")
                        .WithMany("UserRoles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Role");

                    b.Navigation("User");
                });

            modelBuilder.Entity("CloudBackup.Model.Backup", b =>
                {
                    b.Navigation("BackupLogs");

                    b.Navigation("BackupObjects");
                });

            modelBuilder.Entity("CloudBackup.Model.Job", b =>
                {
                    b.Navigation("Configuration")
                        .IsRequired();

                    b.Navigation("JobObjects");
                });

            modelBuilder.Entity("CloudBackup.Model.Profile", b =>
                {
                    b.Navigation("TenantProfiles");
                });

            modelBuilder.Entity("CloudBackup.Model.RestoreJob", b =>
                {
                    b.Navigation("RestoreJobObjects");
                });

            modelBuilder.Entity("CloudBackup.Model.Role", b =>
                {
                    b.Navigation("UserRoles");
                });

            modelBuilder.Entity("CloudBackup.Model.Tenant", b =>
                {
                    b.Navigation("TenantProfiles");

                    b.Navigation("Users");
                });

            modelBuilder.Entity("CloudBackup.Model.User", b =>
                {
                    b.Navigation("UserRoles");
                });
#pragma warning restore 612, 618
        }
    }
}
