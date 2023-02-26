using Microsoft.Extensions.DependencyInjection;
using CloudBackup.Common;
using CloudBackup.Model;

namespace CloudBackup.JobProcessorApp
{
    public class ConfigurationProcessor
    {
        private readonly IServiceProvider _provider = null!;

        public ConfigurationProcessor(IServiceProvider provider)
        {
            _provider = provider;
        }

        public Task RunAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    bool processed = false;

                    while (!processed)
                    {
                        Configuration? configuration;

                        var factory = _provider.GetService<IRepositoryFactory>()!;

                        using (var configurationRepository = factory.GetRepository<Configuration>())
                        {
                            var configurations = await configurationRepository.FindAllAsync(null);

                            configuration = configurations.FirstOrDefault();
                        }

                        if (configuration != null)
                        {
                            if (configuration.ConfigurationStatus == ConfigurationStatus.Processed)
                            {
                                processed = true;
                            }
                            else if (configuration.ConfigurationStatus == ConfigurationStatus.Created)
                            {
                                try
                                {
                                    var adminRole = new Role
                                    {
                                        Name = "Global Administrator",
                                        IsGlobalAdmin = true,
                                        ProfileRights = Permissions.ReadWrite,
                                        TenantRights = Permissions.ReadWrite,
                                        UserRights = Permissions.ReadWrite,
                                        InfrastructureRights = Permissions.ReadWrite,
                                        BackupRights = Permissions.ReadWrite,
                                        RestoreRights = Permissions.ReadWrite,
                                        SecurityRights = Permissions.ReadWrite
                                    };

                                    var tenantAdminRole = new Role
                                    {
                                        Name = "Tenant Administrator",
                                        IsUserAdmin = true,
                                        ProfileRights = Permissions.ReadWrite,
                                        TenantRights = Permissions.Read,
                                        UserRights = Permissions.ReadWrite,
                                        InfrastructureRights = Permissions.ReadWrite,
                                        BackupRights = Permissions.ReadWrite,
                                        RestoreRights = Permissions.ReadWrite,
                                        SecurityRights = Permissions.ReadWrite
                                    };

                                    var fullAccessRole = new Role
                                    {
                                        Name = "Full Access",
                                        ProfileRights = Permissions.Read,
                                        TenantRights = Permissions.Read,
                                        InfrastructureRights = Permissions.ReadWrite,
                                        BackupRights = Permissions.ReadWrite,
                                        RestoreRights = Permissions.ReadWrite,
                                        SecurityRights = Permissions.ReadWrite
                                    };

                                    var backupRole = new Role
                                    {
                                        Name = "Backup",
                                        ProfileRights = Permissions.Read,
                                        TenantRights = Permissions.Read,
                                        InfrastructureRights = Permissions.Read,
                                        BackupRights = Permissions.ReadWrite,
                                        RestoreRights = Permissions.Read,
                                        SecurityRights = Permissions.Read
                                    };

                                    var readOnlyRole = new Role
                                    {
                                        Name = "Read Only",
                                        ProfileRights = Permissions.Read,
                                        TenantRights = Permissions.Read,
                                        InfrastructureRights = Permissions.Read,
                                        BackupRights = Permissions.Read,
                                        RestoreRights = Permissions.Read,
                                        SecurityRights = Permissions.Read
                                    };

                                    var roles = new[] { adminRole, tenantAdminRole, fullAccessRole, backupRole, readOnlyRole };

                                    using (var roleRepository = factory.GetRepository<Role>())
                                    {
                                        roleRepository.AddRange(roles);
                                        await roleRepository.SaveChangesAsync();
                                    }

                                    var tenant = new Tenant
                                    {
                                        Name = "System tenant",
                                        Description = "This is system tenant that isn't visible to end user.",
                                        IsSystem = true
                                    };

                                    var passwordHashed = PasswordHelper.GetPasswordHashSalt(configuration.Password);

                                    var user = new User
                                    {
                                        IsEnabled = true,
                                        Name = configuration.UserName,
                                        Login = configuration.UserName,
                                        Tenant = tenant,
                                        Description = "Administrator",
                                        PasswordSalt = passwordHashed.Salt,
                                        PasswordHash = passwordHashed.Hash,
                                        Email = configuration.Email,
                                        UtcOffsetTicks = configuration.UtcOffsetTicks,
                                        UserRoles = new List<UserRole>()
                                    };
                                    user.UserRoles.Add(new UserRole { RoleId = adminRole.Id, User = user });

                                    using var userRepository = factory.GetRepository<User>();
                                    userRepository.Add(user);

                                    await userRepository.SaveChangesAsync();

                                    configuration.ConfigurationStatus = ConfigurationStatus.Processed;
                                    processed = true;
                                }
                                catch
                                {
                                    configuration.ConfigurationStatus = ConfigurationStatus.Failed;
                                }
                                finally
                                {
                                    using var configRepository = factory.GetRepository<Configuration>();
                                    configuration.Password = string.Empty;
                                    configRepository.Update(configuration);

                                    await configRepository.SaveChangesAsync();
                                }
                            }
                        }
                        else
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }
    }
}
