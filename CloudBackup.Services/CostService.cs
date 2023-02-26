using Microsoft.Extensions.Caching.Memory;
using CloudBackup.Common;
using CloudBackup.Common.ScheduleParams;
using CloudBackup.Model;
using CloudBackup.Repositories;

namespace CloudBackup.Services
{
    public interface ICostService
    {
        Task<List<JobCost>> GetJobMonthlyCost(int userId, IReadOnlyList<CostJobInputData> inputData);
    }

    public class CostService : ICostService
    {
        private readonly IMemoryCache _cache;
        private static readonly MemoryCacheEntryOptions PriceCacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(7));
        private static readonly MemoryCacheEntryOptions JobObjectsCacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
        private static readonly object StoragePricesCacheLock = new();
        private static readonly object JobObjectsCacheLock = new();
        private const double _dailyDataChangeRatio = 0.03;
        private const double _usedDiskSpaceRatio = 0.7;
        private const double _compressionRatio = 0.6;
        private static readonly TimeSpan CostPeriod = TimeSpan.FromDays(30);
        private readonly IRepository<Profile> _profileRepository;
        private readonly IComputeCloudClientFactory _computeCloudFactory = null!;

        public CostService(IMemoryCache memoryCache, IRepository<Profile> profileRepository, IComputeCloudClientFactory computeCloudFactory)
        {
            _cache = memoryCache;
            _profileRepository = profileRepository;
            _computeCloudFactory = computeCloudFactory;
        }

        public async Task<List<JobCost>> GetJobMonthlyCost(int userId, IReadOnlyList<CostJobInputData> inputData)
        {
            var result = new List<JobCost>();

            if (inputData.Count == 0)
            {
                return result;
            }

            var jobObjectsByInputData = inputData.ToDictionary(x => x, x => x.JobObjects.ToHashSet());
            var allJobObjects = jobObjectsByInputData.SelectMany(x => x.Value).Distinct().ToList();

            if (allJobObjects.Count == 0)
            {
                return inputData.Select(x => new JobCost()).ToList();
            }

            var priceData = GetJobPriceData(allJobObjects);
            var objectStorageSizes = await GetJobObjectStorageSizes(userId, allJobObjects);

            foreach (var data in inputData)
            {
                var jobObjects = jobObjectsByInputData[data];

                if (jobObjects.Count == 0)
                    continue;

                var backupsLifetimeData = GetBackupLifetimes(data.Schedule, data.RetentionPolicy, data.ReplicationOptions);

                var jobCost = CalculateJobCost(jobObjects, backupsLifetimeData, objectStorageSizes, priceData.StoragePrices!);

                result.Add(jobCost);
            }

            return result;
        }

        private BackupsLifetimeData GetBackupLifetimes(Schedule schedule, RetentionPolicy retentionPolicy, ReplicationOptions replicationOptions)
        {
            var result = new BackupsLifetimeData();

            var retentionLength = retentionPolicy.GetTimeSpan();
            result.ArchiveCaculatedLifetime = retentionLength;

            if (schedule.StartupType == StartupType.Immediately || schedule.StartupType == StartupType.Delayed)
            {
                result.CalculationPeriod = CostPeriod.Max(retentionLength);
                result.BackupLifetimes = new List<BackupLifetime>
                {
                    new BackupLifetime
                    {
                        StartedInCalculatedRange = true,
                        IsReplication = replicationOptions.EnableReplication,
                        CalculatedLifetime = CostPeriod.Min(retentionLength),
                    }
                };
                return result;
            }

            IScheduleParam scheduleParam = null!;

            if (string.IsNullOrEmpty(schedule.Params))
            {
                if (schedule.StartupType == StartupType.Delayed)
                {
                    scheduleParam = new DelayedScheduleParam();
                }
                else if (schedule.StartupType == StartupType.Recurring)
                {
                    if (schedule.OccurType == OccurType.Periodically)
                    {
                        scheduleParam = new IntervalScheduleParam
                        {
                            TimeIntervalValue = 1
                        };
                    }
                    else if (schedule.OccurType == OccurType.Daily)
                    {
                        scheduleParam = new DailyScheduleParam
                        {
                            Days = { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday }
                        };
                    }
                    else if (schedule.OccurType == OccurType.Monthly)
                    {
                        scheduleParam = new MonthlyScheduleParam
                        {
                            DayOfMonth = 1,
                            MonthList = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }
                        };
                    }
                }
            }
            else
            {
                scheduleParam = ScheduleParamFactory.CreateScheduleParam(schedule)!;
            }

            TimeSpan minCalculationPeriod;

            switch (scheduleParam!.RecurringPeriodType)
            {
                case RecurringPeriodType.Day:
                    minCalculationPeriod = TimeSpan.FromDays(1);
                    break;
                case RecurringPeriodType.Week:
                    minCalculationPeriod = TimeSpan.FromDays(7);
                    break;
                case RecurringPeriodType.Month:
                    minCalculationPeriod = TimeSpan.FromDays(30);
                    break;
                case RecurringPeriodType.Year:
                    minCalculationPeriod = TimeSpan.FromDays(365);
                    break;
                default:
                    minCalculationPeriod = CostPeriod;
                    break;
            }

            // We calculate max possible cost, so we need to have at least N existing backups,
            // where N = restore points to keep (retention interval).
            // To do this, we split calculation on two phases: warmup and calculation.
            // Warmup is to make sure that initial pool of backups are created.
            // Both phases are finished when:
            // 1) maxExecutionsCount is reached (later backups will replace existing, so no point to continue further)
            // 2) minCalculationPeriod is reached. This is needed for accounting large gaps (for example, monthly Jan-Feb-Mar with retention interval = 1).
            // It is the shortest period that will be repeated
            var jobExecutionDates = new List<DateTime>();
            var calculationStartDate = DateTime.UtcNow.BeginOfMonth();
            var calculationEndDate = new DateTime();
            var maxExecutionsCount = Math.Max(retentionPolicy.RestorePointsToKeep, replicationOptions.BackupInverval);

            var currentRunDate = calculationStartDate;
            var currentExecutionsCount = 0;
            var warmupCompleted = false;
            var calculationCompleted = false;

            while (!calculationCompleted)
            {
                currentRunDate = scheduleParam.GetNextRun(currentRunDate);
                currentExecutionsCount++;

                // Check if phase completed
                if (currentExecutionsCount > maxExecutionsCount && currentRunDate - calculationStartDate >= minCalculationPeriod)
                {
                    if (!warmupCompleted)
                    {
                        // Start calculation phase
                        warmupCompleted = true;
                        calculationStartDate = jobExecutionDates.Last();
                        currentExecutionsCount = 1;
                    }
                    else
                    {
                        // Finished
                        calculationCompleted = true;
                        calculationEndDate = currentRunDate;
                    }
                }

                jobExecutionDates.Add(currentRunDate);
            }

            var backupLifetimes = jobExecutionDates
                .Select((startDate, i) =>
                {
                    var retentionByCountIndex = i + retentionPolicy.RestorePointsToKeep;
                    var countRetentionDate = retentionByCountIndex < jobExecutionDates.Count
                        ? jobExecutionDates[retentionByCountIndex]
                        : calculationEndDate;
                    var retentionByTimeDate = startDate.Add(retentionLength);
                    var endDate = countRetentionDate.Min(retentionByTimeDate);

                    var calculatedLifetime = endDate.Min(calculationEndDate) - startDate.Max(calculationStartDate);
                    var timeSinceLastBackup = i > 0
                        ? startDate - jobExecutionDates[i - 1]
                        : TimeSpan.Zero;

                    var isReplication = i % replicationOptions.BackupInverval == 0;
                    var lastReplicatedBackupIndex = i - 1 - (i - 1) % replicationOptions.BackupInverval;
                    var timeSinceLastReplicatedBackup = lastReplicatedBackupIndex >= 0
                        ? startDate - jobExecutionDates[lastReplicatedBackupIndex]
                        : TimeSpan.Zero;

                    var backupLifetime = new BackupLifetime
                    {
                        StartedInCalculatedRange = startDate.Between(calculationStartDate, calculationEndDate),
                        IsReplication = isReplication,
                        CalculatedLifetime = calculatedLifetime,
                        TimeSinceLastBackup = timeSinceLastBackup,
                        TimeSinceLastReplicatedBackup = timeSinceLastReplicatedBackup
                    };
                    return backupLifetime;
                })
                .Where(x => x.CalculatedLifetime > TimeSpan.Zero)
                .ToList();

            result.BackupLifetimes = backupLifetimes;
            result.CalculationPeriod = calculationEndDate - calculationStartDate;

            return result;
        }

        private static UnitPrice GetStoragePrice(JobObjectType jobObjectType)
        {
            UnitPrice price = null!;

            //https://cloud..ru/prices
            switch (jobObjectType)
            {
                case JobObjectType.Instance:
                case JobObjectType.Volume:
                    price = new UnitPrice { Unit = "GB-Mo", Price = 1.8M };
                    break;
                default:
                    break;
            }

            return price;
        }

        private static JobCost CalculateJobCost(IEnumerable<JobObjectInfo> jobObjects,
            BackupsLifetimeData backupsLifetimeData,
            Dictionary<JobObjectInfo, double> jobObjectStorageSizes,
            Dictionary<JobObjectType, UnitPrice> storagePrices)
        {
            var costDetails = new List<JobObjectsCostDetails>();

            foreach (var jobObjectsByType in jobObjects.GroupBy(x => x.Type))
            {
                var jobObjectType = jobObjectsByType.Key;

                decimal storageCost = 0;

                foreach (var jobObject in jobObjectsByType)
                {
                    jobObjectStorageSizes.TryGetValue(jobObject, out var jobObjectStorageSize);

                    // Snapshots are incremental except DynamoDB - On-Demand Backup is entire table
                    var backupsIncremental = false;
                    double currentDailyDataChangeRatio = 1;
                    double currentCompressionRatio = 1;
                    double currentUsedDiskSpaceRatio = 1;

                    for (var i = 0; i < backupsLifetimeData.BackupLifetimes!.Count; i++)
                    {
                        var backupLifetime = backupsLifetimeData.BackupLifetimes[i];
                        var isInitialIncrementalBackup = backupsIncremental && i == 0;

                        // TODO it is possible to replace this two ratios by one - 
                        // (decimal)(backupLifetime.CalculatedLifetime.TotalHours / backupsLifetimeData.CalculationPeriod.TotalHours);
                        var backupLifeTimeRatio = (decimal)(backupLifetime.CalculatedLifetime.TotalHours / CostPeriod.TotalHours);
                        var normalizationRatio = (decimal)(CostPeriod.TotalHours / backupsLifetimeData.CalculationPeriod.TotalHours);

                        var snapshotSize = jobObjectStorageSize;
                        snapshotSize *= currentCompressionRatio;
                        snapshotSize *= currentUsedDiskSpaceRatio;

                        var replicationSnapshotSize = snapshotSize;
                        if (!isInitialIncrementalBackup) // First incremental backup is initial so no need to apply coefficient
                        {
                            var incrementalChangeRatio = currentDailyDataChangeRatio;
                            snapshotSize *= incrementalChangeRatio;
                            replicationSnapshotSize *= Math.Min(currentDailyDataChangeRatio * backupLifetime.TimeSinceLastReplicatedBackup.TotalDays, 1);
                        }

                        // Calculation formula for storage is: price * gbMonths
                        // gbMonths = snapshotSize * backupMonthDuration / monthDuration
                        // backupMonthDuration = backupDuration * normalizationRatio
                        // normalizationRatio = monthDuration / calculationDuration
                        var storagePrice = storagePrices.GetValueOrDefault(jobObjectType) ?? new UnitPrice();

                        storageCost += storagePrice.Price * (decimal)snapshotSize * backupLifeTimeRatio * normalizationRatio;
                    }
                }

                // Add costs to results
                var storageCurrency = storagePrices.Values.Select(x => x.Currency).FirstOrDefault()!;

                if (storageCost != 0)
                {
                    costDetails.Add(new JobObjectsCostDetails
                    {
                        JobObjectType = jobObjectType,
                        OperationType = CostOperationType.Storage,
                        Cost = new Cost(storageCost, storageCurrency)
                    });
                }
            }

            return new JobCost
            {
                CostDetails = costDetails.ToArray(),
                DailyDataChangeRatio = _dailyDataChangeRatio,
                UsedDiskSpaceRatio = _usedDiskSpaceRatio,
                CompressionRatio = _compressionRatio
            };
        }

        private UnitPrice GetStoragePriceFromCache(JobObjectType jobObjectType)
        {
            var cacheKey = $"price_storage_{jobObjectType}";
            UnitPrice price;

            lock (StoragePricesCacheLock)
            {
                if (!_cache.TryGetValue(cacheKey, out price))
                {
                    price = GetStoragePrice(jobObjectType);
                    _cache.Set(cacheKey, price, PriceCacheEntryOptions);
                }
            }

            return price;
        }

        private JobPriceData GetJobPriceData(IReadOnlyCollection<JobObjectInfo> jobObjects)
        {
            var storagePrices = new Dictionary<JobObjectType, UnitPrice>();
            var replicationTransferPrices = new Dictionary<(string sourceRegion, string targetRegion), UnitPrice>();
            var inboundTransferPrices = new Dictionary<string, UnitPrice>();
            var outboundTransferPrices = new Dictionary<string, UnitPrice>();
            var lambdaPrices = new Dictionary<string, UnitPrice>();

            foreach (var jobObjectsGroup in jobObjects.GroupBy(x => new { x.FolderId, x.Type }))
            {
                var jobObjectType = jobObjectsGroup.Key.Type;

                var key = jobObjectType;
                if (!storagePrices.ContainsKey(key))
                {
                    var storagePrice = GetStoragePriceFromCache(jobObjectType);
                    storagePrices.Add(key, storagePrice);
                }
            }

            return new JobPriceData { StoragePrices = storagePrices };
        }

        private async Task<Dictionary<JobObjectInfo, double>> GetJobObjectStorageSizes(int userId, IEnumerable<JobObjectInfo> jobObjects)
        {
            var result = new Dictionary<JobObjectInfo, double>();

            foreach (var group in jobObjects.GroupBy(x => new { x.Type, x.FolderId, x.ProfileId }))
            {
                var objectType = group.Key.Type;
                var profileId = group.Key.ProfileId;
                var folderId = group.Key.FolderId;
                var objectsIds = group.Select(x => x.ObjectId);
                var storageSizes = await GetJobObjectStorageSizesFromCache(userId, folderId, profileId, objectType, objectsIds);

                foreach (var jobObject in group)
                {
                    result.Add(jobObject, storageSizes.GetValueOrDefault(jobObject.ObjectId));
                }
            }

            return result;
        }

        private async Task<Dictionary<string, double>> GetJobObjectStorageSizesFromCache(int userId, string folderId, int profileId, JobObjectType objectType, IEnumerable<string> objectIds)
        {
            string GetCacheKey(string objectId) => $"JobCost_JobObjectSize_{folderId}_{profileId}_{objectType}_{objectId}";

            var result = new Dictionary<string, double>();
            var objectIdsToProcess = new List<string>();

            foreach (var objectId in objectIds)
            {
                var cacheKey = GetCacheKey(objectId);

                if (_cache.TryGetValue(cacheKey, out double storageSizeCached))
                {
                    result[objectId] = storageSizeCached;
                }
                else
                {
                    objectIdsToProcess.Add(objectId);
                }
            }

            // Get not cached data
            if (objectIdsToProcess.Count > 0)
            {
                var storageSizes = await GetJobObjectStorageSizes(folderId, profileId, objectType, objectIdsToProcess);

                foreach (var objectId in objectIdsToProcess)
                {
                    double storageSize = 0;

                    if (storageSizes.ContainsKey(objectId))
                    {
                        storageSize = storageSizes[objectId];
                        var cacheKey = GetCacheKey(objectId);

                        lock (JobObjectsCacheLock)
                        {
                            if (!_cache.TryGetValue(cacheKey, out double storageSizeCached))
                            {
                                _cache.Set(cacheKey, storageSize, JobObjectsCacheEntryOptions);
                            }
                        }
                    }

                    if (!result.ContainsKey(objectId))
                    {
                        result[objectId] = storageSize;
                    }
                }
            }

            return result;
        }

        private async Task<Dictionary<string, double>> GetJobObjectStorageSizes(string folderId, int profileId, JobObjectType objectType, IEnumerable<string> objectIds)
        {
            var items = new Dictionary<string, double>();
            var profile = await _profileRepository.FindByIdAsync(profileId, null);
            var credentials = new CloudCredentials { KeyId = profile.KeyId, PrivateKey = profile.PrivateKey, ServiceAccountId = profile.ServiceAccountId };
            var cloudClient = _computeCloudFactory.CreateComputeCloudClient(credentials);
            var diskList = await cloudClient.GetDiskList(folderId);

            if (diskList == null || diskList.Disks == null)
            {
                return items;
            }

            switch (objectType)
            {
                case JobObjectType.Instance:
                    var instanceList = await cloudClient.GetInstanceList(folderId);

                    if (instanceList != null && instanceList.Instances != null)
                    {
                        var instances = instanceList.Instances.Where(x => objectIds.Contains(x.Id)).ToList();

                        foreach (var instance in instances)
                        {
                            var bootDisk = diskList.Disks.FirstOrDefault(x => x.Id == instance!.BootDisk!.DiskId);

                            items[instance.Id!] = 0;

                            if (bootDisk != null && !string.IsNullOrEmpty(bootDisk.Size))
                            {
                                items[instance.Id!] += double.Parse(bootDisk!.Size!) / (1024 * 1024 * 1024);
                            }

                            if (instance!.SecondaryDisks != null)
                            {
                                var diskIds = instance!.SecondaryDisks.Select(x => x.DiskId).ToList();
                                var secondaryDisks = diskList.Disks.Where(x => diskIds.Contains(x.Id));

                                foreach (var secondaryDisk in secondaryDisks)
                                {
                                    items[instance.Id!] += double.Parse(secondaryDisk!.Size!) / (1024 * 1024 * 1024);
                                }
                            }
                        }
                    }
                    break;
                case JobObjectType.Volume:
                    var disks = diskList.Disks.Where(x => objectIds.Contains(x.Id));

                    foreach (var disk in disks)
                    {
                        items[disk!.Id!] = double.Parse(disk!.Size!) / (1024 * 1024 * 1024);
                    }

                    break;

                default:
                    var message = "ObjectStorageSizes not defined";
                    throw new ArgumentOutOfRangeException(message);
            }

            return items;
        }

        private class JobPriceData
        {
            public Dictionary<JobObjectType, UnitPrice>? StoragePrices { get; set; }
        }

        private class BackupsLifetimeData
        {
            public List<BackupLifetime>? BackupLifetimes { get; set; }
            public TimeSpan CalculationPeriod { get; set; }
            public TimeSpan ArchiveCaculatedLifetime { get; set; }
        }

        private class BackupLifetime
        {
            public bool StartedInCalculatedRange { get; set; }
            public bool IsReplication { get; set; }
            public TimeSpan CalculatedLifetime { get; set; }
            public TimeSpan TimeSinceLastBackup { get; set; }
            public TimeSpan TimeSinceLastReplicatedBackup { get; set; }
        }
    }

    public class UnitPrice
    {
        public string? Unit { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "₽";

        public static UnitPrice operator +(UnitPrice price1, UnitPrice price2)
        {
            if (price1 == null && price2 == null)
            {
                var errorMessage = "prices are not defined";
                throw new ArgumentNullException(errorMessage);
            }

            if (price1 == null) return price2;
            if (price2 == null) return price1;

            if (price1.Unit != price2.Unit)
                throw new ArgumentException("Items can not have different unit type.");
            if (price1.Currency != price2.Currency)
                throw new ArgumentException("Items can not have different currency.");

            return new UnitPrice
            {
                Currency = price1.Currency,
                Unit = price1.Unit,
                Price = price1.Price + price2.Price
            };
        }
    }

    public struct Cost
    {
        public decimal Value { get; set; }
        public string Currency { get; set; }

        public Cost(decimal value, string currency) : this()
        {
            Value = value;
            Currency = currency;
        }
    }

    public enum CostOperationType
    {
        [Description("Backups storage")]
        Storage,
        [Description("Archived backups storage")]
        GlacierStorage,
        [Description("Replicated backups storage")]
        ReplicationStorage,
        [Description("Replication transfer")]
        ReplicationTransfer,
        [Description("Archive operations")]
        ArchiveOperation,
        [Description("Inbound transfer")]
        InboundTransfer,
        [Description("Cross-cloud replication")]
        CrossCloudReplication
    }

    public struct CostJobInputData
    {
        public List<JobObjectInfo> JobObjects { get; set; }

        public Schedule Schedule { get; set; }

        public RetentionPolicy RetentionPolicy { get; set; }

        public ReplicationOptions ReplicationOptions { get; set; }
    }

    public struct JobObjectInfo
    {
        public string ObjectId { get; set; }
        public JobObjectType Type { get; set; }
        public string FolderId { get; set; } = null!;
        public int ProfileId { get; set; }

        public JobObjectInfo(JobObject jobObject)
        {
            ObjectId = jobObject.ObjectId;
            Type = jobObject.Type;
            FolderId = jobObject.FolderId;
            ProfileId = jobObject.ProfileId;
        }
    }

    public class JobCost
    {
        public JobObjectsCostDetails[] CostDetails { get; set; } = new JobObjectsCostDetails[0];

        public double DailyDataChangeRatio { get; set; }
        public double UsedDiskSpaceRatio { get; set; }
        public double CompressionRatio { get; set; }
    }

    public class JobObjectsCostDetails
    {
        public JobObjectType JobObjectType { get; set; }

        public CostOperationType OperationType { get; set; }

        public Cost Cost { get; set; }
    }

    public enum S3StorageType
    {
        Standart = 0,
        Glacier = 1,
        DeepArchive = 2
    }
}
