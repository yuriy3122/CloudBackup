using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CloudBackup.Common;
using CloudBackup.Model;
using CloudBackup.Repositories;

namespace CloudBackup.JobProcessorApp
{
    public class AlertsProcessor
    {
        private const int MaxAlertsCount = 10000;
        private readonly IServiceProvider _provider = null!;
        private static readonly Expression<Func<User, bool>> FilterHasBackupRightsRead = user => user.UserRoles.Any(x => x.Role.BackupRights.HasFlag(Permissions.Read));
        private static readonly Expression<Func<User, bool>> FilterHasRestoreRightsRead = user => user.UserRoles.Any(x => x.Role.RestoreRights.HasFlag(Permissions.Read));

        public AlertsProcessor(IServiceProvider provider)
        {
            _provider = provider;
        }

        public Task RunAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    var tasks = new List<Task>
                    {
                        DeleteOldAlerts(),
                        DeleteOldLogEntries(),
                        ProcessNewAlerts()
                    };

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }

        private async Task DeleteOldLogEntries()
        {
            try
            {
                while (true)
                {
                    var orderBy = nameof(Log.EventDate) + "[desc]";
                    var page = new EntitiesPage(100000, 2);
                    Expression<Func<Log, bool>> filter = f => f.Id > 0;

                    var factory = _provider.GetService<IRepositoryFactory>()!;

                    using (var logRepository = factory.GetRepository<Log>())
                    {
                        var logEntries = (await logRepository.FindAsync(filter, orderBy, page, null)).ToList();

                        foreach (var logEntry in logEntries)
                        {
                            logRepository.Remove(logEntry);
                        }

                        if (logEntries.Any())
                        {
                            await logRepository.SaveChangesAsync();
                        }
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
            catch (Exception)
            {
            }
        }

        private async Task ProcessNewAlerts()
        {
            try
            {
                while (true)
                {
                    var factory = _provider.GetService<IRepositoryFactory>()!;

                    using (var alertRepository = factory.GetRepository<Alert>())
                    {
                        var alerts = (await alertRepository.FindAsync(f => f.IsProcessed == false, null, null, null)).ToList();

                        foreach (var alert in alerts)
                        {
                            if (alert.SourceObjectType == nameof(Job))
                            {
                                using var jobRepository = factory.GetRepository<Job>();
                                var job = await jobRepository.FindByIdAsync(alert.SourceObjectId, null);

                                if (job != null)
                                {
                                    var userIds = await GetUserIds(job.TenantId, FilterHasBackupRightsRead);

                                    using var userAlertRepository = factory.GetRepository<UserAlert>();
                                    foreach (var userId in userIds)
                                    {
                                        userAlertRepository.Add(new UserAlert()
                                        {
                                            IsNew = true,
                                            UserId = userId,
                                            AlertId = alert.Id
                                        });
                                    }

                                    await userAlertRepository.SaveChangesAsync();
                                }
                            }
                            else if (alert.SourceObjectType == nameof(RestoreJob))
                            {
                                using var restoreJobRepository = factory.GetRepository<RestoreJob>();
                                var restoreJob = await restoreJobRepository.FindByIdAsync(alert.SourceObjectId, null);

                                if (restoreJob != null)
                                {
                                    var userIds = await GetUserIds(restoreJob.TenantId, FilterHasRestoreRightsRead);

                                    using var userAlertRepository = factory.GetRepository<UserAlert>();
                                    foreach (var userId in userIds)
                                    {
                                        userAlertRepository.Add(new UserAlert()
                                        {
                                            IsNew = true,
                                            UserId = userId,
                                            AlertId = alert.Id
                                        });
                                    }

                                    await userAlertRepository.SaveChangesAsync();
                                }
                            }

                            alert.IsProcessed = true;

                            alertRepository.Update(alert);

                            await alertRepository.SaveChangesAsync();
                        }
                    }

                    await Task.Delay(1000);
                }
            }
            catch (Exception)
            {
            }
        }

        private async Task DeleteOldAlerts()
        {
            try
            {
                while (true)
                {
                    var factory = _provider.GetService<IRepositoryFactory>()!;

                    using (var alertRepository = factory.GetRepository<Alert>())
                    {
                        var orderBy = nameof(Alert.Date) +"[desc]";
                        var page = new EntitiesPage(MaxAlertsCount, 2);
                        var alerts = (await alertRepository.FindAsync(f => f.IsProcessed, orderBy, page, null)).ToList();

                        foreach (var alert in alerts)
                        {
                            alertRepository.Remove(alert);
                        }

                        if (alerts.Any())
                        {
                            await alertRepository.SaveChangesAsync();
                        }
                    }
                    
                    await Task.Delay(1000);
                }
            }
            catch (Exception)
            {
            }
        }

        private async Task<int[]> GetUserIds(int tenantId, Expression<Func<User, bool>> filterByRights)
        {
            var factory = _provider.GetService<IRepositoryFactory>()!;

            using var userRepository = factory.GetRepository<User>();

            Expression<Func<User, bool>> filterByTenantId = user => user.TenantId == tenantId;

            var users = await userRepository.FindAsync(filterByTenantId.And(filterByRights), null, null, query => query.Include(x => x.UserRoles).ThenInclude(x => x.Role));

            return users.Select(x => x.Id).ToArray();
        }
    }
}
