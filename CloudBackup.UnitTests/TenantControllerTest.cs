using Moq;
using Xunit;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using CloudBackup.Model;
using CloudBackup.Repositories;
using CloudBackup.Common;
using CloudBackup.Services;
using CloudBackup.Management.Controllers;

namespace CloudBackup.UnitTests
{
    public class TenantControllerTest
    {
        [Fact]
        public void CanGetTenants()
        {
            var tenants = new[]
            {
                new Tenant {Id = 1, Name = "tenant company-1", RowVersion = Array.Empty<byte>(), Description = "desc1"},
                new Tenant {Id = 2, Name = "tenant company-2", RowVersion = Array.Empty<byte>(), Description = "desc1"}
            };

            var role = new Role { Id = 1, IsGlobalAdmin = true, TenantRights = Permissions.Read };

            var currentUser = new User
            {
                Id = 1,
                Login = "admin",
                Name = "Administrator",
                RowVersion = Array.Empty<byte>(),
                TenantId = tenants[0].Id,
                Tenant = tenants[0],
                UserRoles = new List<UserRole> { new UserRole { UserId = 1, RoleId = role.Id, Role = role } }
            };

            var permissions = new UserPermissions { IsGlobalAdmin = true, TenantRights = Permissions.Read, UserRights = Permissions.Read };

            var serviceProvider = BuildServiceProvider(tenants, currentUser, permissions);

            var controller = serviceProvider.GetService<TenantController>();

            var result = controller?.GetTenants(null, null, string.Empty, string.Empty).GetAwaiter().GetResult();

            var model = result?.Value;
            Assert.NotNull(model);

            Assert.Equal(model?.Items.Count, tenants.Length);
            Assert.Equal(model?.TotalCount, tenants.Length);

            foreach (var pair in tenants.Zip(model?.Items!, (x, y) => new { Source = x, Result = y }))
            {
                Assert.Equal(pair.Source.Id, pair.Result.Id);
                Assert.Equal(Convert.ToBase64String(pair.Source.RowVersion), pair.Result.RowVersion);
                Assert.Equal(pair.Source.Name, pair.Result.Name);
                Assert.Equal(pair.Source.Description, pair.Result.Description);

                if (pair?.Source == currentUser.Tenant)
                {
                    Assert.Equal(1, pair?.Result?.Admins?.Count);
                    Assert.Equal(currentUser.Id, pair?.Result?.Admins?[0].Id);
                }
            }
        }

        [Fact]
        public async Task CanAddTenant()
        {
            var currentUser = new User { Id = 1, Login = "test", TenantId = 1, Description = "desc1" };
            var newTenant = new TenantViewModel
            {
                Name = "Administrator",
                Description = "desc1"
            };

            var serviceProvider = BuildServiceProvider(
                Array.Empty<Tenant>(),
                currentUser,
                new UserPermissions { IsGlobalAdmin = true, TenantRights = Permissions.Write });

            var controller = serviceProvider.GetService<TenantController>()!;

            var result = await controller.AddTenant(newTenant);
            var resultTenantViewModel = result.Value;

            var repository = serviceProvider.GetService<IRepository<Tenant>>();
            var tenants = await repository!.FindAllAsync(null);
            var tenant = tenants.Single(x => x.Name == newTenant.Name);

            Assert.True(tenant != null);
            Assert.Equal(newTenant.Description, tenant?.Description);
        }

        [Fact]
        public async Task CanDeleteTenant()
        {
            var tenant = new Tenant { Id = 1, Name = "test1", RowVersion = Array.Empty<byte>() };
            var currentUser = new User
            {
                Id = 1,
                Login = "user1",
                Name = "User 1",
                RowVersion = Array.Empty<byte>(),
                Tenant = tenant
            };

            var serviceProvider = BuildServiceProvider(
                new[] { tenant },
                currentUser,
                new UserPermissions { TenantRights = Permissions.Write });

            var controller = serviceProvider.GetService<TenantController>();

            await controller!.RemoveTenant(tenant.Id);

            var tenantRepository = serviceProvider.GetService<IRepository<Tenant>>();
            var newTenants = await tenantRepository!.FindAllAsync(null);
            Assert.False(newTenants.Any());
        }

        private static IServiceProvider BuildServiceProvider(IReadOnlyCollection<Tenant> tenants, User currentUser, UserPermissions permissions)
        {
            var tenantRepository = new MockRepository<Tenant>(tenants);

            var userRepository = new Mock<MockRepository<User>>(new List<User> { currentUser }).As<IUserRepository>();
            userRepository.CallBase = true;
            userRepository.Setup(i => i.GetUserByLoginAsync(currentUser.Login)).ReturnsAsync(currentUser);

            var roleRepository = new Mock<IRoleRepository>();
            roleRepository.Setup(i => i.GetPermissionsByLoginAsync(currentUser.Login)).ReturnsAsync(permissions);
            roleRepository.Setup(i => i.GetPermissionsByUserIdAsync(currentUser.Id)).ReturnsAsync(permissions);

            var jobRepository = new Mock<MockRepository<Job>> { CallBase = true }.As<IRepository<Job>>();
            var restoreJobRepository = new Mock<MockRepository<RestoreJob>> { CallBase = true }.As<IRepository<RestoreJob>>();

            IRepository<TenantProfile> tenantProfileRepository = new MockRepository<TenantProfile>(
                tenants.SelectMany(x => x.TenantProfiles.EmptyIfNull()));

            IRepository<Profile> profileRepository = new MockRepository<Profile>(
                tenants.SelectMany(tenant => tenant.TenantProfiles.EmptyIfNull().Select(x => x.Profile).Where(x => x != null)));

            var schedule = new Schedule() { Name = "Daily" };
            var scheduleRepository = new Mock<MockRepository<Schedule>>(new List<Schedule> { schedule }).As<IRepository<Schedule>>();

            var logRepository = new Mock<MockRepository<Log>> { CallBase = true }.As<IRepository<Log>>();

            var collection = new ServiceCollection();
            collection.AddScoped<IRepository<Tenant>>(x => tenantRepository);
            collection.AddScoped<ITenantService, TenantService>();
            collection.AddScoped(x => roleRepository.Object);
            collection.AddScoped(x => userRepository.Object);
            collection.AddScoped<IRepository<User>>(provider => provider.GetService<IUserRepository>()!);
            collection.AddScoped<IUserService, UserService>();
            collection.AddScoped(x => jobRepository.Object);
            collection.AddScoped(x => restoreJobRepository.Object);
            collection.AddScoped(x => tenantProfileRepository);
            collection.AddScoped(x => profileRepository);
            collection.AddScoped<IRepository<Role>>(provider => provider.GetService<IRoleRepository>()!);
            collection.AddScoped(x => logRepository.Object);
            collection.AddScoped(x => scheduleRepository.Object);

            collection.AddScoped(provider =>
            {
                var controller = new TenantController(
                    provider.GetRequiredService<ITenantService>(),
                    provider.GetRequiredService<IUserService>(),
                    provider.GetRequiredService<IUserRepository>(),
                    provider.GetRequiredService<IRoleRepository>(),
                    provider.GetRequiredService<IRepository<TenantProfile>>());

                UnitTestsHelper.SetUserIdentity(controller, currentUser);

                return controller;
            });

            return collection.BuildServiceProvider();
        }
    }
}