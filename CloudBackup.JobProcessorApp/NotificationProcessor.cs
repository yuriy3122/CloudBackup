using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CloudBackup.Common;
using CloudBackup.Model;
using CloudBackup.Services;

namespace CloudBackup.JobProcessorApp
{
    public class NotificationProcessor
    {
        private CancellationTokenSource _cancellationTokenSource = null!;
        private readonly TimeSpan _checkDelay = TimeSpan.FromMinutes(2);
        private HashSet<int> _processedFailedBackupIds = null!;
        private DateTime? _alertLastCheck;
        private DateTime? _dailySummaryLastCheck;
        private readonly IServiceProvider _provider = null!;

        public NotificationProcessor(IServiceProvider provider)
        {
            _provider = provider;
            _processedFailedBackupIds = new HashSet<int>();
        }

        public Task RunAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    await ProcessNotifications(_cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }

        private async Task ProcessNotifications(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.WhenAll(SendAlertNotifications(), SendDailySummaryNotifications());
                }
                catch
                {
                }

                await Task.Delay(_checkDelay, cancellationToken);
            }
        }

        private async Task SendAlertNotifications()
        {
            try
            {
                if (_alertLastCheck == null)
                {
                    _alertLastCheck = DateTime.UtcNow - _checkDelay;
                }

                List<Backup> currentFailedBackups;
                var factory = _provider.GetService<IRepositoryFactory>()!;

                using (var backupRepository = factory.GetRepository<Backup>())
                {
                    currentFailedBackups = (await backupRepository.FindAsync(x => x.Status == BackupStatus.Failed && x.FinishedAt >= _alertLastCheck, null, null,
                        i => i.Include(x => x.BackupObjects).Include(x => x.Job))).ToList();
                }

                _alertLastCheck = DateTime.UtcNow;

                var newFailedBackups = currentFailedBackups.Where(x => !_processedFailedBackupIds.Contains(x.Id));
                var failedBackupsByJobs = newFailedBackups.ToLookup(x => x.Job);
                _processedFailedBackupIds = currentFailedBackups.Select(x => x.Id).ToHashSet();

                var sendTasks = new List<Task>();
                var failedJobsByTenantId = failedBackupsByJobs.Select(x => x.Key).GroupBy(x => x.TenantId);

                foreach (var tenantJobs in failedJobsByTenantId)
                {
                    var messageBuilder = new StringBuilder();
                    messageBuilder.AppendLine("There were errors while processing backup jobs:");

                    foreach (var job in tenantJobs)
                    {
                        messageBuilder.AppendLine();
                        messageBuilder.AppendLine($"{job.Name} (Objects: {job.ObjectCount})");

                        foreach (var backup in failedBackupsByJobs[job])
                        {
                            messageBuilder.AppendLine($"• {backup.StatusText} ({backup.StartedAt} - {backup.FinishedAt} UTC)");
                        }
                    }

                    sendTasks.Add(SendNotification(tenantJobs.Key, NotificationType.Alert, "Backup Alert", messageBuilder.ToString()));
                }

                await Task.WhenAll(sendTasks);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Alert notifications processing canceled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Alert notifications processing failed: {ex.Message}");
            }
        }

        private async Task SendDailySummaryNotifications()
        {
            try
            {
                List<NotificationConfiguration> notificationConfigurations;
                var factory = _provider.GetService<IRepositoryFactory>()!;

                using (var repository = factory.GetRepository<NotificationConfiguration>())
                {
                    notificationConfigurations = (await repository.FindAsync(x => x.Type == NotificationType.DailySummary &&
                        x.IsEnabled, null, null, query => query.Include(x => x.Tenant))).ToList();
                }

                var sendTasks = new List<Task>();

                foreach (var notificationConfiguration in notificationConfigurations)
                {
                    if (_dailySummaryLastCheck == null)
                    {
                        _dailySummaryLastCheck = DateTime.UtcNow - _checkDelay;
                    }

                    if (!notificationConfiguration.SendTime.Between(_dailySummaryLastCheck.Value, DateTime.UtcNow))
                    {
                        continue;
                    }

                    _dailySummaryLastCheck = DateTime.UtcNow;

                    var subject = " Backup Daily Summary";
                    var message = await GetDailySummary(notificationConfiguration.Tenant!);
                    var messageBuilder = new StringBuilder(message);

                    if (notificationConfiguration.IncludeTenants)
                    {
                        if (!notificationConfiguration.Tenant!.IsSystem)
                        {
                            throw new NotSupportedException("Can't send 'Include Tenants' notification for non-system tenant.");
                        }

                        List<Tenant> includedTenants;

                        using (var tenantRepository = factory.GetRepository<Tenant>())
                        {
                            includedTenants = (await tenantRepository.FindAsync(x => x.Id != notificationConfiguration.TenantId &&
                                !x.Isolated, null, null, null)).ToList();
                        }

                        foreach (var includedTenant in includedTenants)
                        {
                            var newMessage = await GetDailySummary(includedTenant);
                            messageBuilder.AppendLine();
                            messageBuilder.AppendLine();
                            messageBuilder.AppendLine();
                            messageBuilder.AppendLine(newMessage);
                        }
                    }

                    sendTasks.Add(SendNotification(notificationConfiguration.Tenant!.Id, NotificationType.DailySummary, subject, messageBuilder.ToString()));
                }

                await Task.WhenAll(sendTasks);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("SendDailySummaryNotifications notifications processing canceled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendDaily notifications processing failed: {ex.Message}");
            }
        }

        private async Task<string> GetDailySummary(Tenant tenant)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-1).AddMinutes(-10);

            List<Job> jobs;
            var factory = _provider.GetService<IRepositoryFactory>()!;

            using (var client = factory.GetRepository<Job>())
            {
                jobs = (await client.FindAsync(x => x.TenantId == tenant.Id, null, null, query => query.Include(x => x.JobObjects))).ToList();
            }

            var activeJobs = jobs.Where(x => x.LastRunAt >= startDate && x.LastRunAt <= endDate).ToDictionary(x => x.Id);

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Daily Summary for tenant \"{tenant.Name}\" ({DateTime.UtcNow.Date:d})");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Total backup jobs: {jobs.Count}");
            messageBuilder.AppendLine($"Executed backup jobs: {activeJobs.Count}");

            if (activeJobs.Count == 0)
            {
                return messageBuilder.ToString();
            }

            List<Backup> backups;
            ILookup<int, Backup> jobIdToBackups;
            var activeJobIds = activeJobs.Keys.ToList()!;
            using (var repository = factory.GetRepository<Backup>())
            {
                backups = (await repository.FindAsync(x => activeJobIds.Contains(x.JobId) && x.StartedAt <= endDate && 
                    x.FinishedAt >= startDate, null, null, i => i.Include(x => x.BackupObjects))).ToList();

                jobIdToBackups = backups.ToLookup(x => x.JobId);
            }

            var completedJobsCount = backups.Count(x => x.Status == BackupStatus.Success || x.Status == BackupStatus.Warning);
            var failedJobsCount = backups.Count(x => x.Status == BackupStatus.Failed);

            messageBuilder.AppendLine($"Backups: {backups.Count} (completed: {completedJobsCount}, failed: {failedJobsCount})");

            if (!jobIdToBackups.Any())
            {
                return messageBuilder.ToString();
            }

            messageBuilder.AppendLine();
            messageBuilder.AppendLine("Backups status:");

            foreach (var jobBackupsGroup in jobIdToBackups)
            {
                var job = activeJobs[jobBackupsGroup.Key];
                var jobBackups = jobBackupsGroup.ToList();

                if (jobBackups.Count == 0)
                {
                    continue;
                }

                messageBuilder.AppendLine($"• {job.Name} (Objects: {job.ObjectCount})");

                foreach (var backup in jobBackups)
                {
                    messageBuilder.AppendLine($"    • {backup.StatusText} ({backup.StartedAt} - {backup.FinishedAt} UTC)");
                }
            }

            return messageBuilder.ToString();
        }

        private async Task SendNotification(int tenantId, NotificationType type, string subject, string message)
        {
            var factory = _provider.GetService<IRepositoryFactory>()!;
            using var repository = factory.GetRepository<NotificationConfiguration>();
            var notificationService = new NotificationService(repository);

            await notificationService.Send(tenantId, type, subject, message);
        }
    }
}