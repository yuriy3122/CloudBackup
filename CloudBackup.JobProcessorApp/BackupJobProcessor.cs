using System.Collections.Concurrent;
using System.Linq.Expressions;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CloudBackup.Common;
using CloudBackup.Model;

namespace CloudBackup.JobProcessorApp
{
    public class BackupJobProcessor
    {
        private const int ConcurrentJobCount = 4;
        private const int ConcurrentSnapshotCount = 5;
        private readonly ConcurrentQueue<int> _jobQueue = new();
        private CancellationTokenSource _cancellationTokenSource = null!;
        private readonly IServiceProvider _provider = null!;

        private static readonly SemaphoreSlim _semaphore = new(ConcurrentSnapshotCount, ConcurrentSnapshotCount);

        public BackupJobProcessor(IServiceProvider provider)
        {
            _provider = provider;
        }

        public Task RunAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    _cancellationTokenSource = new CancellationTokenSource();

                    var tasks = new List<Task>
                    {
                        UpdateJobQueue(_cancellationTokenSource.Token)
                    };

                    for (int i = 0; i < ConcurrentJobCount; i++)
                    {
                        tasks.Add(ProcessJobFromQueue(i * 100, _cancellationTokenSource.Token));
                    }

                    tasks.Add(ApplyRetentionPolicy(_cancellationTokenSource.Token));

                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }

        private async Task UpdateJobQueue(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var jobIds = await GetJobIds();

                    foreach (var jobId in jobIds)
                    {
                        if (!_jobQueue.Contains(jobId))
                        {
                            _jobQueue.Enqueue(jobId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                await Task.Delay(1000, token);
            }
        }

        private async Task<List<int>> GetJobIds()
        {
            var ids = new List<int>();

            var factory = _provider.GetService<IRepositoryFactory>()!;

            Expression<Func<Job, bool>> filter = f => f.Status == JobStatus.Idle &&
                                                      (f.RunNow || f.NextRunAt == null || f.NextRunAt < DateTime.UtcNow) &&
                                                      (f.Schedule.StartupType == StartupType.Recurring ||
                                                      (f.Schedule.StartupType != StartupType.Recurring && (f.RunNow || f.RunDelayed || f.LastRunAt == null)));

            using (var jobRepository = factory.GetRepository<Job>())
            {
                if (jobRepository == null)
                {
                    throw new ArgumentNullException("JobRepository is null");
                }

                var order = string.Empty;
                static IQueryable<Job> Includes(IQueryable<Job> i) => i.Include(p => p.Schedule);

                ids = (await jobRepository.FindAsync(filter, order, null, Includes)).Select(x => x.Id).ToList();
            }

            return ids;
        }

        private async Task ApplyRetentionPolicy(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(60 * 1000, cancellationToken);

                    var factory = _provider.GetService<IRepositoryFactory>()!;

                    using (var repository = factory.GetRepository<RestoreJob>())
                    {
                        var restoreJobsToProcess = await repository.FindAsync(x => x.Status != RestoreJobStatus.Complete, string.Empty, null, null);

                        if (restoreJobsToProcess.Any())
                        {
                            continue;
                        }
                    }

                    List<Backup> backups;
                    using (var repository = factory.GetRepository<Backup>())
                    {
                        Expression<Func<Backup, bool>> filter = x =>
                            !x.IsPermanent &&
                            !x.IsArchive &&
                            x.FinishedAt != null &&
                            x.Status != BackupStatus.Running &&
                            x.Status != BackupStatus.Replicating;

                        static IQueryable<Backup> Includes(IQueryable<Backup> i) => i.Include(p => p.Job.Configuration).Include(p => p.BackupObjects).ThenInclude(p => p.Profile);

                        backups = (await repository.FindAsync(filter, null, null, Includes)).ToList();
                    }

                    var backupsToDelete = new Dictionary<int, Backup>();

                    var jobGroups = backups.GroupBy(x => x.JobId);

                    var serializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

                    foreach (var jobGroup in jobGroups)
                    {
                        var jobConfiguration = jobGroup.First().Job.Configuration;
                        var options = JsonConvert.DeserializeObject<JobOptions>(jobConfiguration.Configuration, serializerSettings)!;
                        var expirationDateTime = DateTime.UtcNow - options.RetentionPolicy.GetTimeSpan();

                        var backupsCount = 0;

                        foreach (var backup in jobGroup.OrderByDescending(x => x.FinishedAt))
                        {
                            if (backup.Status != BackupStatus.Failed)
                            {
                                backupsCount++;
                            }

                            if (backupsCount > options.RetentionPolicy.RestorePointsToKeep && !backupsToDelete.ContainsKey(backup.Id))
                            {
                                backupsToDelete.Add(backup.Id, backup);
                            }
                        }

                        var backupsExpiredByDate = jobGroup.Where(x => x.FinishedAt < expirationDateTime);

                        foreach (var backupExpiredByDate in backupsExpiredByDate)
                        {
                            if (!backupsToDelete.ContainsKey(backupExpiredByDate.Id))
                            {
                                backupsToDelete.Add(backupExpiredByDate.Id, backupExpiredByDate);
                            }
                        }
                    }

                    var computeCloudFactory = _provider.GetService<IComputeCloudClientFactory>()!;

                    foreach (var backupToDelete in backupsToDelete.Values)
                    {
                        try
                        {
                            var settings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
                            var jobOptions = JsonConvert.DeserializeObject<JobOptions>(backupToDelete.JobConfiguration, settings);

                            foreach (var backupObject in backupToDelete.BackupObjects)
                            {
                                try
                                {
                                    if (backupObject.Type == BackupObjectType.Snapshot)
                                    {
                                        var credentials = new CloudCredentials
                                        {
                                            KeyId = backupObject.Profile.KeyId,
                                            PrivateKey = backupObject.Profile.PrivateKey,
                                            ServiceAccountId = backupObject.Profile.ServiceAccountId
                                        };

                                        var cloudClient = computeCloudFactory.CreateComputeCloudClient(credentials);

                                        await cloudClient.DeleteSnapshot(backupObject.DestObjectId);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }

                            using var repository = factory.GetRepository<Backup>();
                            var backup = await repository.FindByIdAsync(backupToDelete.Id, null);

                            if (backup != null)
                            {
                                repository.Remove(backup);

                                await repository.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private async Task ProcessJobFromQueue(int startDelay, CancellationToken cancellationToken)
        {
            await Task.Delay(startDelay, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _jobQueue.TryDequeue(out int jobId);

                    await ProcessJob(jobId, cancellationToken);
                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                await Task.Delay(1000, cancellationToken);
            }
        }

        private static DateTime GetJobNextRunAt(Job job)
        {
            DateTime dateTime = DateTime.UtcNow;

            if (job.Schedule.StartupType != StartupType.Immediately && string.IsNullOrEmpty(job.Schedule.Params))
            {
                throw new ArgumentException("Job.Schedule.Params");
            }
            if (job.Schedule.StartupType == StartupType.Delayed || job.Schedule.StartupType == StartupType.Recurring)
            {
                var scheduleParam = ScheduleParamFactory.CreateScheduleParam(job.Schedule);

                if (scheduleParam != null && job != null)
                {
                    dateTime = scheduleParam.GetScheduledDateTime(job.NextRunAt) ?? dateTime;
                }
            }

            return dateTime;
        }

        private static bool IsPermittedToRunJob(Job job)
        {
            var scheduleParam = ScheduleParamFactory.CreateScheduleParam(job.Schedule);

            return scheduleParam == null || scheduleParam.IsPermittedToRunNow();
        }

        private async Task ProcessJob(int jobId, CancellationToken cancellationToken)
        {
            if (jobId == 0)
            {
                return;
            }

            Job? job = null;

            var factory = _provider.GetService<IRepositoryFactory>()!;

            using (var jobRepository = factory.GetRepository<Job>())
            {
                if (jobRepository != null)
                {
                    job = await jobRepository.FindByIdAsync(jobId, i => i.Include(p => p.Schedule)
                                                                         .Include(p => p.Configuration)
                                                                         .Include(p => p.JobObjects).ThenInclude(p => p.Profile));

                    if (job.Status != JobStatus.Idle)
                    {
                        return;
                    }
                }
            }

            try
            {
                if (job != null)
                {
                    var runAt = job.NextRunAt;

                    if (!job.RunNow && runAt == null)
                    {
                        runAt = GetJobNextRunAt(job);
                        job.NextRunAt = runAt;
                    }

                    using (var jobRepository = factory.GetRepository<Job>())
                    {
                        if (jobRepository != null)
                        {
                            jobRepository.Update(job);
                            await jobRepository.SaveChangesAsync();
                        }
                    }

                    if (job.RunNow || (runAt < DateTime.UtcNow.AddSeconds(1) && IsPermittedToRunJob(job)))
                    {
                        try
                        {
                            if (!job.RunNow)
                            {
                                job.NextRunAt = GetJobNextRunAt(job);
                                job.RunDelayed = false;
                            }

                            job.LastRunAt = DateTime.UtcNow;
                            job.RunNow = false;
                            job.Status = JobStatus.Running;

                            using var jobRepository = factory.GetRepository<Job>();

                            if (jobRepository != null)
                            {
                                jobRepository.Update(job);
                                await jobRepository.SaveChangesAsync();
                            }

                            await ProcessJobObjects(job, cancellationToken);
                        }
                        finally
                        {
                            Job? jobEntity = null;

                            using var jobRepository = factory.GetRepository<Job>();

                            if (jobRepository != null)
                            {
                                jobEntity = await jobRepository.FindByIdAsync(job.Id, null);

                                if (jobEntity != null)
                                {
                                    jobEntity.Status = JobStatus.Idle;
                                    await jobRepository.SaveChangesAsync();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task ProcessJobObjects(Job job, CancellationToken cancellationToken)
        {
            var settings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
            var jobOptions = JsonConvert.DeserializeObject<JobOptions>(job.Configuration.Configuration, settings);

            var backup = new Backup
            {
                JobId = job.Id,
                Name = job.Name,
                StartedAt = DateTime.UtcNow,
                Status = BackupStatus.Running,
                IsArchive = false,
                IsPermanent = false,
                JobConfiguration = job.Configuration.Configuration
            };

            var factory = _provider.GetService<IRepositoryFactory>()!;

            using (var backupRepository = factory.GetRepository<Backup>())
            {
                if (backupRepository != null)
                {
                    try
                    {
                        backupRepository.Add(backup);

                        await backupRepository.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            var logData = string.Empty;

            try
            {
                await ProcessDiskJobObjects(job.JobObjects, backup.Id);
            }
            catch (Exception ex)
            {
                backup.Status = BackupStatus.Failed;
                logData = JsonConvert.SerializeObject(ex.ToExceptionInfo());
            }
            finally
            {
                using (var backupRepository = factory.GetRepository<Backup>())
                {
                    var backupEntity = await backupRepository.FindByIdAsync(backup.Id, null);
                    backupEntity.FinishedAt = DateTime.UtcNow;
                    backupEntity.Status = backup.Status == BackupStatus.Failed ? BackupStatus.Failed : BackupStatus.Success;

                    await backupRepository.SaveChangesAsync();
                }

                var message = backup?.Status == BackupStatus.Failed ? "has failed" : "has finished successfully";

                using (var logRepository = factory.GetRepository<Log>())
                {
                    var logEntity = new Log
                    {
                        EventDate = DateTime.UtcNow,
                        MessageText = $"Backup job {message}",
                        XmlData = logData,
                        Severity = backup?.Status == BackupStatus.Failed ? Severity.Error : Severity.Info,
                        ObjectType = nameof(Backup),
                        ObjectId = (backup?.Id ?? 0).ToString(),
                        UserId = job?.UserId
                    };

                    logRepository.Add(logEntity);
                    await logRepository.SaveChangesAsync();
                }

                using var alertRepository = factory.GetRepository<Alert>();

                alertRepository.Add(new Alert()
                {
                    Type = backup?.Status == BackupStatus.Failed ? AlertType.Error : AlertType.Info,
                    Date = DateTime.UtcNow,
                    IsProcessed = false,
                    Message = $"Backup job \"{job?.Name}\" {message}",
                    Subject = backup?.Status == BackupStatus.Failed ? "Backup job failed" : "Backup job finished",
                    SourceObjectId = job?.Id ?? 0,
                    SourceObjectType = nameof(Job)
                });

                await alertRepository.SaveChangesAsync();
            }
        }

        private async Task ProcessDiskJobObjects(IReadOnlyCollection<JobObject> jobObjects, int backupId)
        {
            var backupObjects = new List<BackupObject>();

            var computeCloudFactory = _provider.GetService<IComputeCloudClientFactory>()!;

            foreach (var jobObject in jobObjects)
            {
                var credentials = new CloudCredentials
                {
                    KeyId = jobObject.Profile.KeyId,
                    PrivateKey = jobObject.Profile.PrivateKey,
                    ServiceAccountId = jobObject.Profile.ServiceAccountId
                };

                var cloudClient = computeCloudFactory.CreateComputeCloudClient(credentials);

                if (jobObject.Type == JobObjectType.Instance)
                {
                    var instances = await cloudClient.GetInstanceList(jobObject.FolderId);
                    var instance = instances?.Instances?.FirstOrDefault(x => x.Id == jobObject.ObjectId);

                    if (instance == null)
                    {
                        var errorMessage = $"instance {jobObject.ObjectId} not found";
                        throw new ArgumentNullException(errorMessage);
                    }

                    var disks = new List<Disk>();

                    if (instance.BootDisk != null)
                    {
                        disks.Add(instance.BootDisk);
                    }
                    if (instance.SecondaryDisks != null)
                    {
                        disks.AddRange(instance.SecondaryDisks);
                    }

                    foreach (var disk in disks)
                    {
                        if (disk != null)
                        {
                            var diskList = await cloudClient.GetDiskList(jobObject.FolderId);
                            var diskDescription = diskList?.Disks?.FirstOrDefault(x => x.Id == disk.DiskId);

                            if (diskDescription != null)
                            {
                                backupObjects.Add(new BackupObject
                                {
                                    BackupId = backupId,
                                    FolderId = diskDescription.FolderId ?? string.Empty,
                                    BackupParams = JsonConvert.SerializeObject(new BackupOptions(diskDescription, instance)),
                                    ProfileId = jobObject.ProfileId,
                                    Region = instance.ZoneId ?? string.Empty,
                                    ParentId = instance.Id ?? string.Empty,
                                    SourceObjectId = disk?.DiskId ?? string.Empty,
                                    Type = BackupObjectType.Snapshot
                                });
                            }
                        }
                    }
                }
                else if (jobObject.Type == JobObjectType.Volume)
                {
                    var diskDescriptionList = await cloudClient.GetDiskList(jobObject.FolderId);
                    var diskDescription = diskDescriptionList?.Disks?.FirstOrDefault(x => x.Id == jobObject.ObjectId);

                    if (diskDescription != null)
                    {
                        backupObjects.Add(new BackupObject
                        {
                            BackupId = backupId,
                            FolderId = diskDescription?.FolderId ?? string.Empty,
                            BackupParams = JsonConvert.SerializeObject(new BackupOptions(diskDescription ?? new DiskDescription())),
                            ProfileId = jobObject.ProfileId,
                            Region = diskDescription?.ZoneId ?? string.Empty,
                            ParentId = string.Empty,
                            SourceObjectId = diskDescription?.Id ?? string.Empty,
                            Type = BackupObjectType.Snapshot
                        });
                    }
                    else
                    {
                        var errorMessage = $"disk {jobObject.ObjectId} not found";
                        throw new ArgumentNullException(errorMessage);
                    }
                }
            }

            var tasks = new List<Task>(backupObjects.Count);

            foreach (var backupObject in backupObjects)
            {
                tasks.Add(ProcessBackupObject(backupObject));
            }

            await Task.WhenAll(tasks);
        }

        private async Task ProcessBackupObject(BackupObject backupObject)
        {
            try
            {
                await _semaphore.WaitAsync();

                Profile? profile = null;
                var factory = _provider.GetService<IRepositoryFactory>()!;

                using (var profileRepository = factory.GetRepository<Profile>())
                {
                    if (profileRepository != null)
                    {
                        profile = await profileRepository.FindByIdAsync(backupObject.ProfileId, null);
                    }
                }

                if (profile == null)
                {
                    var message = $"Profile for object {backupObject.SourceObjectId} is null";
                    throw new ArgumentNullException(message);
                }

                var credentials = new CloudCredentials
                {
                    KeyId = profile.KeyId,
                    PrivateKey = profile.PrivateKey,
                    ServiceAccountId = profile.ServiceAccountId
                };

                var computeCloudFactory = _provider.GetService<IComputeCloudClientFactory>()!;
                var cloudClient = computeCloudFactory.CreateComputeCloudClient(credentials);

                if (!string.IsNullOrEmpty(backupObject.FolderId) && !string.IsNullOrEmpty(backupObject.SourceObjectId))
                {
                    backupObject.StartedAt = DateTime.UtcNow;

                    var snapshot = await cloudClient.CreateSnapshot(backupObject.FolderId, backupObject.SourceObjectId);

                    backupObject.FinishedAt = DateTime.UtcNow;
                    backupObject.DestObjectId = snapshot?.Id ?? string.Empty;
                    backupObject.Status = snapshot != null ? BackupObjectStatus.Success : BackupObjectStatus.Failed;

                    using var backupObjectRepository = factory.GetRepository<BackupObject>();

                    if (backupObjectRepository != null)
                    {
                        backupObjectRepository.Add(backupObject);

                        await backupObjectRepository.SaveChangesAsync();
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}