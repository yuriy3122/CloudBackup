using Moq;
using Xunit;
using System;
using CloudBackup.Model;
using CloudBackup.Repositories;
using CloudBackup.Services;
using CloudBackup.Management.Controllers;

namespace CloudBackup.UnitTests
{
    public class JobControllerTest
    {
        [Fact]
        public void GetJobs()
        {
            var user = new User
            {
                Id = 2,
                Login = "admin",
                Name = "Admin",
                UtcOffset = new TimeSpan(3, 0, 0),                
            };

            var permissions = new UserPermissions {UserRights = Permissions.Read, BackupRights = Permissions.Read};

            var userRepository = new Mock<MockRepository<User>>((object)new[] {user}).As<IUserRepository>();
            userRepository.CallBase = true;
            userRepository.Setup(i => i.GetUserByLoginAsync(user.Login)).ReturnsAsync(user);

            var roleRepository = new Mock<IRoleRepository>();
            roleRepository.Setup(i => i.GetPermissionsByLoginAsync(user.Login)).ReturnsAsync(permissions);
            roleRepository.Setup(i => i.GetPermissionsByUserIdAsync(user.Id)).ReturnsAsync(permissions);

            var restoreJobRepository = new Mock<MockRepository<RestoreJob>> {CallBase = true}.As<IRepository<RestoreJob>>();

            var tenant = new Tenant() { Id = 45, Name = "Service company NJ" };
            var otherTenant = new Tenant() { Id = 99, Name = "Service company LA" };

            var job = new Job
            {
                Id = 2,
                Name = "Daily-backup",
                User = user,
                Tenant = tenant,
                TenantId = tenant.Id,
                RowVersion = Convert.FromBase64String("AAAAAAAAB9U=")
            };
            job.Configuration = new JobConfiguration { JobId = job.Id, Configuration = string.Empty };
            var otherJob = new Job
            {
                Id = 5,
                Name = "Daily-backup-2",
                User = user,
                Tenant = otherTenant,
                TenantId = otherTenant.Id,
                RowVersion = Convert.FromBase64String("AAAAAAAAB9U=")
            };

            var repository = new Mock<MockRepository<Job>>((object)new[] { job, otherJob }) { CallBase = true };

            var tenantService = new Mock<ITenantService>();
            var tenantIds = new[] { tenant.Id };
            tenantService.Setup(i => i.GetAllowedTenantIds(user.Id)).ReturnsAsync(tenantIds);

            var scheduleRepository = new MockRepository<Schedule>();
            var jobService = new JobService(repository.Object, userRepository.Object, roleRepository.Object, tenantService.Object);
            var scheduleService = new ScheduleService(scheduleRepository, repository.Object, userRepository.Object,
                roleRepository.Object, tenantService.Object);

            var profileService = new Mock<IProfileService>();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var controller = new JobController(repository.Object, null, null, userRepository.Object, roleRepository.Object,
                jobService, scheduleService, profileService.Object, null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            UnitTestsHelper.SetUserIdentity(controller, user);

            var response = controller.GetJobs(null, null, null, null);
            response.Wait();

            var model = response.Result.Value;

            Assert.Equal(1, model?.Items.Count);
            Assert.Equal(tenant.Id, model?.Items[0].TenantId);
        }
    }
}