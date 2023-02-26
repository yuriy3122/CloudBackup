using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using CloudBackup.Common;
using CloudBackup.Model;

namespace CloudBackup.JobProcessorApp
{
    public class RestoreJobProcessor
    {
        private const int ConcurrentJobCount = 4;
        private const int ConcurrentDiskRestoreCount = 5;
        private readonly ConcurrentQueue<int> _jobQueue = new();
        private CancellationTokenSource _cancellationTokenSource = null!;
        private readonly IServiceProvider _provider = null!;
        private static readonly SemaphoreSlim _semaphore = new(ConcurrentDiskRestoreCount, ConcurrentDiskRestoreCount);

        public RestoreJobProcessor(IServiceProvider provider)
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

                    var tasks = new List<Task> { UpdateJobQueue(_cancellationTokenSource.Token) };

                    for (int i = 0; i < ConcurrentJobCount; i++)
                    {
                        tasks.Add(ProcessJobFromQueue(i * 100, _cancellationTokenSource.Token));
                    }

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

        private static List<PartitionInfo> GetMountPointsFromPartitionDescription(List<string> description)
        {
            var partitions = new List<PartitionInfo>();

            for (int i = 1; i < description.Count; i++)
            {
                var line = description[i];

                var items = line.Split(' ');

                if (items.Length < 2)
                {
                    continue;
                }

                var pi = new PartitionInfo
                {
                    Name = items.First(),
                    Root = Regex.Replace(items.First(), @"[\d-]", string.Empty)
                };

                for (int j = 1; j < items.Length; j++)
                {
                    if (string.IsNullOrWhiteSpace(items[j])) continue;

                    if (items[j].Contains("disk") || items[j].Contains("part"))
                    {
                        pi.Type = items[j];
                    }
                    else if (items[j].Contains('/'))
                    {
                        pi.MountPoint = items[j];
                    }
                }

                partitions.Add(pi);
            }

            return partitions;
        }

        private async Task UpdateJobQueue(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var jobIds = await GetRestoreJobIds();

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

        private async Task<List<int>> GetRestoreJobIds()
        {
            var ids = new List<int>();

            var factory = _provider.GetService<IRepositoryFactory>()!;

            using (var jobRepository = factory.GetRepository<RestoreJob>())
            {
                if (jobRepository != null)
                {
                    Expression<Func<RestoreJob, bool>> filter = f => f.Status == RestoreJobStatus.Idle;
                    var order = string.Empty;

                    ids = (await jobRepository.FindAsync(filter, order, null, null)).Select(x => x.Id).ToList();
                }
            }

            return ids;
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

                   await ProcessRestoreJob(jobId, cancellationToken);
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

        private async Task ProcessRestoreJob(int jobId, CancellationToken cancellationToken)
        {
            if (jobId == 0)
            {
                return;
            }

            RestoreJob? job = null;

            var factory = _provider.GetService<IRepositoryFactory>()!;

            using (var repository = factory.GetRepository<RestoreJob>())
            {
                if (repository != null)
                {
                    job = await repository.FindByIdAsync(jobId, null);

                    if (job.Status != RestoreJobStatus.Idle)
                    {
                        return;
                    }

                    job.StartedAt = DateTime.UtcNow;
                    job.Status = RestoreJobStatus.Running;

                    await repository.SaveChangesAsync();
                }
            }

            using (var repository = factory.GetRepository<RestoreJob>())
            {
                if (repository != null)
                {
                    job = await repository.FindByIdAsync(jobId, i => i.Include(p => p.RestoreJobObjects).ThenInclude(p => p.BackupObject));
                }
            }

            if (job != null)
            {
                var logData = string.Empty;
                RestoreJobResult result = RestoreJobResult.Success;

                try
                {
                    var tasks = new List<Task>();

                    foreach (var jobObject in job.RestoreJobObjects)
                    {
                        if (jobObject.BackupObject?.Type == BackupObjectType.Snapshot)
                        {
                            tasks.Add(RestoreVolume(jobObject.Id));
                        }
                    }

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    result = RestoreJobResult.Failed;
                    logData = JsonConvert.SerializeObject(ex.ToExceptionInfo());
                }
                finally
                {
                    using (var repository = factory.GetRepository<RestoreJob>())
                    {
                        job = await repository.FindByIdAsync(job.Id, q => q.Include(x => x.RestoreJobObjects));

                        if (job != null)
                        {
                            job.Status = RestoreJobStatus.Complete;
                            job.FinishedAt = DateTime.UtcNow;
                            job.Result = result;

                            repository.Update(job);

                            await repository.SaveChangesAsync();
                        }
                    }

                    var message = job?.Result == RestoreJobResult.Failed ? "has failed" : "has finished successfully";

                    using (var logRepository = factory.GetRepository<Log>())
                    {
                        var logEntity = new Log
                        {
                            EventDate = DateTime.UtcNow,
                            MessageText = $"Restore job {message}",
                            XmlData = logData,
                            Severity = job?.Result == RestoreJobResult.Failed ? Severity.Error : Severity.Info,
                            ObjectType = nameof(RestoreJob),
                            ObjectId = (job?.Id ?? 0).ToString(),
                            UserId = job?.UserId
                        };

                        logRepository.Add(logEntity);
                        await logRepository.SaveChangesAsync();
                    }

                    using var alertRepository = factory.GetRepository<Alert>();

                    alertRepository.Add(new Alert()
                    {
                        Type = job?.Result == RestoreJobResult.Failed ? AlertType.Error : AlertType.Info,
                        Date = DateTime.UtcNow,
                        IsProcessed = false,
                        Message = $"Restore job \"{job?.Name}\" {message}",
                        Subject = "Restore job finished",
                        SourceObjectId = job?.Id ?? 0,
                        SourceObjectType = nameof(RestoreJob)
                    });

                    await alertRepository.SaveChangesAsync();
                }
            }
        }

        private static CreateDiskRequest PrepareCreateDiskRequest(string snapshotId, BackupOptions? backupsParams, RestoreOptions? restoreParams)
        {
            var id = Guid.NewGuid().ToString().Replace("-", "");
            var maxLength = 62 - id.Length;
            var diskName = backupsParams?.Disk?.Name ?? "disk";
            diskName = diskName.Length <= maxLength ? diskName : diskName[..maxLength];

            var createDiskRequest = new CreateDiskRequest
            {
                SnapshotId = snapshotId,
                FolderId = string.IsNullOrEmpty(restoreParams?.FolderId) ? (backupsParams?.Disk?.FolderId ?? "") : (restoreParams?.FolderId ?? ""),
                ZoneId = string.IsNullOrEmpty(restoreParams?.DestPlacement) ? (backupsParams?.Disk?.ZoneId ?? "") : (restoreParams?.DestPlacement ?? ""),
                Name = $"{diskName}-{id}",
                Description = backupsParams?.Disk?.Description ?? string.Empty,
                TypeId = backupsParams?.Disk?.TypeId ?? string.Empty,
                Size = backupsParams?.Disk?.Size != null ? long.Parse(backupsParams.Disk.Size) : 4194304,
                BlockSize = backupsParams?.Disk?.BlockSize != null ? long.Parse(backupsParams.Disk.BlockSize) : 4096
            };

            return createDiskRequest;
        }

        private static CreateInstanceRequest PrepareCreateInstanceRequest(string bootDiskId, Dictionary<string, string> secondaryDiskIds, 
            BackupOptions? backupsParams, RestoreOptions? restoreParams)
        {
            var id = Guid.NewGuid().ToString().Replace("-", "");
            var maxLength = 62 - id.Length;
            var instanceName = backupsParams?.Instance.Name ?? "i";
            instanceName = instanceName.Length <= maxLength ? instanceName : instanceName[..maxLength];

            var resources = backupsParams?.Instance?.Resources;

            var password = string.IsNullOrEmpty(restoreParams?.InstancePassword) ? $"{InstanceMetadata.GetInstanceId()}!" : restoreParams?.InstancePassword;

            var createInstanceRequest = new CreateInstanceRequest
            {
                BootDiskSpec = new DiskCreateInstanceSpecification() { DiskId = bootDiskId },
                Description = backupsParams?.Instance?.Description ?? string.Empty,
                FilesystemSpecs = new List<FileSystem>(),
                FolderId = string.IsNullOrEmpty(restoreParams?.FolderId) ? (backupsParams?.Instance.FolderId ?? "") : (restoreParams?.FolderId ?? ""),
                ZoneId = string.IsNullOrEmpty(restoreParams?.DestPlacement) ? (backupsParams?.Instance?.ZoneId ?? "") : (restoreParams?.DestPlacement ?? ""),
                Hostname = $"{instanceName}-{id}",
                Metadata = new CreateInstanceMetadata() { Userdata = $"#ps1\nnet user administrator \"{password}\"\n" },
                Name = $"{instanceName}-{id}",
                NetworkInterfaceSpecs = new List<NetworkInterfaceSpec>(),
                PlacementPolicy = new object(),
                PlatformId = backupsParams?.Instance?.PlatformId ?? "standard-v3",
                ResourcesSpec = new InstanceResourcesSpec()
                { 
                    CoreFraction = resources != null ? long.Parse(resources.CoreFraction!) : 100,
                    Cores = resources != null ? int.Parse(resources.Cores!) : 2,
                    Memory = resources != null ? long.Parse(resources.Memory!) : 2 * 1024 * 1024,
                },
                SecondaryDiskSpecs = new List<DiskSpecification>()
            };

            if (backupsParams?.Instance?.NetworkInterfaces != null)
            {
                foreach (var networkInterface in backupsParams?.Instance?.NetworkInterfaces!)
                {
                    createInstanceRequest.NetworkInterfaceSpecs.Add(new NetworkInterfaceSpec
                    {
                        SubnetId = networkInterface.SubnetId,
                        SecurityGroupIds = new List<string>(),
                        PrimaryV4AddressSpec = new IpAddressSpec()
                        {
                            DnsRecordSpecs = new List<DnsRecord>(),
                            OneToOneNatSpec = new OneToOneNatSpec() { IpVersion = networkInterface.PrimaryV4Address?.OneToOneNat?.IpVersion }
                        }
                    });
                }
            }

            var secondaryDisks = backupsParams?.Instance?.SecondaryDisks;

            if (secondaryDisks != null)
            {
                foreach (var secondaryDisk in secondaryDisks)
                {
                    var diskId = secondaryDisk?.DiskId ?? string.Empty;

                    if (secondaryDiskIds.ContainsKey(diskId))
                    {
                        var diskSpecification = new DiskSpecification
                        {
                            DiskId = secondaryDiskIds[diskId],
                            AutoDelete = secondaryDisk?.AutoDelete ?? false,
                            DeviceName = secondaryDisk?.DeviceName ?? string.Empty,
                            Mode = secondaryDisk?.Mode ?? string.Empty
                        };

                        createInstanceRequest.SecondaryDiskSpecs.Add(diskSpecification);
                    }
                }
            }

            return createInstanceRequest;
        }

        private async Task RestoreVolume(int id)
        {
            try
            {
                await _semaphore.WaitAsync();

                RestoreJobObject? restoreJobObject = null;
                var factory = _provider.GetService<IRepositoryFactory>()!;

                using (var profileRepository = factory.GetRepository<RestoreJobObject>())
                {
                    if (profileRepository != null)
                    {
                        restoreJobObject = await profileRepository.FindByIdAsync(id, i => i.Include(p => p.BackupObject));
                    }
                }

                BackupOptions? backupsParams = null;

                if (restoreJobObject != null && restoreJobObject.BackupObject != null)
                {
                    backupsParams = JsonConvert.DeserializeObject<BackupOptions>(restoreJobObject.BackupObject.BackupParams);
                }

                RestoreOptions? restoreParams = null;

                if (restoreJobObject != null && restoreJobObject.RestoreParams != null)
                {
                    restoreParams = JsonConvert.DeserializeObject<RestoreOptions>(restoreJobObject.RestoreParams);

                    if (restoreParams?.DestProfileId != null && restoreParams.DestProfileId.Value == 0)
                    {
                        restoreParams.DestProfileId = null;
                    }
                }

                Profile? profile = null;

                using (var profileRepository = factory.GetRepository<Profile>())
                {
                    if (profileRepository != null && restoreJobObject != null && restoreJobObject.BackupObject != null)
                    {
                        if (restoreParams != null && restoreParams.DestProfileId != null)
                        {
                            profile = await profileRepository.FindByIdAsync(restoreParams.DestProfileId.Value, null);
                        }

                        if (profile == null)
                        {
                            profile = await profileRepository.FindByIdAsync(restoreJobObject.BackupObject.ProfileId, null);
                        }
                    }
                }

                var credentials = new CloudCredentials
                {
                    KeyId = profile?.KeyId,
                    PrivateKey = profile?.PrivateKey,
                    ServiceAccountId = profile?.ServiceAccountId
                };

                var computeCloudFactory = _provider.GetService<IComputeCloudClientFactory>()!;
                var cloudClient = computeCloudFactory.CreateComputeCloudClient(credentials);

                if (!string.IsNullOrEmpty(restoreJobObject?.BackupObject?.FolderId))
                {
                    restoreJobObject.StartedAt = DateTime.UtcNow;
                    restoreJobObject.Status = RestoreObjectStatus.Complete;

                    var createDiskRequest = PrepareCreateDiskRequest(restoreJobObject.BackupObject.DestObjectId, backupsParams, restoreParams);
                    var diskResponse = await cloudClient.CreateDisk(createDiskRequest);

                    restoreJobObject.NewObjectId = diskResponse?.Id ?? string.Empty;

                    // for root disk reconstitution, wait for secondary disks to complete
                    if (backupsParams?.Instance != null &&
                        backupsParams?.Instance?.BootDisk?.DiskId == restoreJobObject.BackupObject.SourceObjectId)
                    {
                        bool processing = true;

                        do
                        {
                            using (var repository = factory.GetRepository<RestoreJobObject>())
                            {
                                if (repository != null)
                                {
                                    Expression<Func<RestoreJobObject, bool>> filter = f => f.Id != restoreJobObject.Id &&
                                                                                           f.GroupGuid == restoreJobObject.GroupGuid &&
                                                                                           f.Status == RestoreObjectStatus.Idle;

                                    processing = (await repository.CountAsync(filter, null)) > 0;
                                }
                            }

                            await Task.Delay(1000);
                        }
                        while (processing);

                        var secondaryDisks = new Dictionary<string, string>();

                        using (var repository = factory.GetRepository<RestoreJobObject>())
                        {
                            if (repository != null)
                            {
                                Expression<Func<RestoreJobObject, bool>> filter = f => f.Id != restoreJobObject.Id &&
                                                                                       f.GroupGuid == restoreJobObject.GroupGuid &&
                                                                                       f.Status == RestoreObjectStatus.Complete;
                                var order = string.Empty;
                                static IQueryable<RestoreJobObject> Includes(IQueryable<RestoreJobObject> i) => i.Include(p => p.BackupObject);

                                var restoredJobObjects = (await repository.FindAsync(filter, order, null, Includes)).ToList();

                                foreach (var restoredJobObject in restoredJobObjects)
                                {
                                    if (restoredJobObject != null && restoredJobObject.BackupObject != null)
                                    {
                                        secondaryDisks[restoredJobObject.BackupObject.SourceObjectId] = restoredJobObject.NewObjectId;
                                    }
                                }
                            }
                        }

                        if (backupsParams?.Instance != null && !string.IsNullOrEmpty(restoreParams?.InstanceId) && diskResponse != null)
                        {
                            var bootDiskId = diskResponse.Id ?? string.Empty;
                            var createInstanceRequest = PrepareCreateInstanceRequest(bootDiskId, secondaryDisks, backupsParams, restoreParams);
                            var createInstanceResponse = await cloudClient.CreateInstance(createInstanceRequest);

                            if (createInstanceResponse?.Error != null)
                            {
                                restoreJobObject.Status = RestoreObjectStatus.Failed;
                            }
                        }
                    }

                    restoreJobObject.FinishedAt = DateTime.UtcNow;
                    restoreJobObject.NewObjectId = diskResponse?.Id ?? string.Empty;

                    using var restoreObjectRepository = factory.GetRepository<RestoreJobObject>();

                    if (restoreObjectRepository != null)
                    {
                        restoreObjectRepository.Update(restoreJobObject);

                        await restoreObjectRepository.SaveChangesAsync();
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
