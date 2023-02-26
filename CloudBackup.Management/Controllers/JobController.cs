using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Common;
using CloudBackup.Services;
using CloudBackup.Common.Exceptions;

namespace CloudBackup.Management.Controllers
{
    [Route("Core/Backup/Job")]
    public class JobController : CommonController
    {
        private readonly IRepository<Job> _jobRepository;
        private readonly IRepository<JobObject> _jobObjectRepository;
        private readonly IRepository<BackupObject> _backupObjectRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IJobService _jobService;
        private readonly IScheduleService _scheduleService;
        private readonly IProfileService _profileService;
        private readonly ICostService _costService;
        private readonly ILogger _logger;

        public JobController(IRepository<Job> jobRepository,
                             IRepository<JobObject> jobObjectRepository,
                             IRepository<BackupObject> backupObjectRepository,
                             IUserRepository userRepository,
                             IRoleRepository roleRepository,
                             IJobService jobService,
                             IScheduleService scheduleService,
                             IProfileService profileService,
                             ICostService costService,
                             ILogger logger) : base(userRepository)
        {
            _jobRepository = jobRepository;
            _backupObjectRepository = backupObjectRepository;
            _roleRepository = roleRepository;
            _jobObjectRepository = jobObjectRepository;
            _jobService = jobService;
            _scheduleService = scheduleService;
            _profileService = profileService;
            _costService = costService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<JobViewModelList>> GetJobs(int? pageSize, int? pageNum, string? order, string? filter)
        {
            if (order != null)
            {
                order = Regex.Replace(order, nameof(JobViewModel.TenantName), $"{nameof(Job.Tenant)}.{nameof(Tenant.Name)}", RegexOptions.IgnoreCase);
            }

            var options = new QueryOptions<Job>(new EntitiesPage(pageSize?? short.MaxValue, pageNum ?? 1), order ?? string.Empty, filter ?? string.Empty);
            var jobs = await _jobService.GetJobsAsync(CurrentUser.Id, options);

            var jobList = GetJobViewModelList(CurrentUser, jobs.Items, jobs.TotalCount);

            return jobList;
        }

        [HttpGet]
        [Route("{id:int}")]
        public async Task<ActionResult<JobViewModel>> GetJob(int id)
        {
            var job = await _jobService.GetJobAsync(CurrentUser.Id, id);

            var jobViewModel = GetJobViewModel(CurrentUser, job);

            return jobViewModel;
        }

        [HttpPost("Forced")]
        public async Task<IActionResult> RunForcedJob(string jobIds, bool runDelayed)
        {
            if (string.IsNullOrEmpty(jobIds))
            {
                return NoContent();
            }

            var ids = JsonConvert.DeserializeObject<List<int>>(jobIds);
            var jobs = await _jobRepository.FindAsync(f => ids!.Contains(f.Id), null, null, i => i.Include(p => p.Schedule));

            foreach (var job in jobs)
            {
                if (job.Status != JobStatus.Running)
                {
                    job.RunNow = true;
                    job.RunDelayed = job.Schedule.StartupType == StartupType.Delayed && runDelayed &&
                        (job.NextRunAt > DateTime.UtcNow || job.NextRunAt == null);

                    _jobRepository.Update(job);
                }
            }

            await _jobRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<JobViewModel>> AddJob([FromBody] JobViewModel jobViewModel)
        {
            if (jobViewModel == null)
            {
                throw new ArgumentNullException(nameof(jobViewModel));
            }

            var user = await CheckUserPermissions(Permissions.Write);

            var jobOptions = GetJobOptions(jobViewModel.JobOptions);

            var job = new Job
            {
                Name = jobViewModel.Name ?? string.Empty,
                ObjectCount = jobViewModel.JobObjects != null ? jobViewModel.JobObjects.Count : 0,
                Type = jobOptions.ReplicationOptions.EnableReplication ? JobType.BackupAndReplication : JobType.Backup,
                TenantId = jobViewModel.TenantId ?? 0,
                Description = jobViewModel.Description ?? string.Empty,
                Status = string.IsNullOrEmpty(jobViewModel.Status) ? JobStatus.Idle : (JobStatus)Enum.Parse(typeof(JobStatus), jobViewModel.Status),
                Configuration = new JobConfiguration { Configuration = jobViewModel.JobOptions ?? string.Empty },
                JobObjects = new List<JobObject>(),
                UserId = user.Id
            };

            if (jobViewModel.Schedule != null)
            {
                await UpdateScheduleForJob(user, job, jobViewModel);
            }

            await AddNewJobObjects(job, jobViewModel.JobObjects);

            _jobRepository.Add(job);

            await _jobRepository.SaveChangesAsync();

            var newJobViewModel = new JobViewModel(job, user.UtcOffset);

            return CreatedAtAction(nameof(GetJob), new { id = job.Id }, newJobViewModel);
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> UpdateJob(int id, [FromBody] JobViewModel jobViewModel)
        {
            if (jobViewModel == null)
            {
                throw new ArgumentNullException(nameof(jobViewModel));
            }

            var user = await CheckUserPermissions(Permissions.Write);

            var job = await _jobRepository.FindByIdAsync(id,
                i => i.Include(p => p.JobObjects).Include(p => p.Schedule).Include(p => p.Configuration).Include(p => p.User));

            if (job != null)
            {
                var jobOptions = GetJobOptions(jobViewModel.JobOptions);

                job.Name = jobViewModel.Name ?? string.Empty;
                job.Type = jobOptions.ReplicationOptions.EnableReplication ? JobType.BackupAndReplication : JobType.Backup;
                job.TenantId = jobViewModel.TenantId ?? 0;
                job.Description = jobViewModel.Description ?? string.Empty;

                if (job.Configuration == null)
                {
                    job.Configuration = new JobConfiguration { JobId = job.Id, Configuration = JsonConvert.SerializeObject(jobOptions) };
                }
                else
                {
                    job.Configuration.Configuration = jobViewModel.JobOptions ?? string.Empty;
                }

                job.Status = string.IsNullOrEmpty(jobViewModel.Status) ?
                    job.Status : (JobStatus)Enum.Parse(typeof(JobStatus), jobViewModel.Status);

                if (jobViewModel.Schedule != null)
                {
                    await UpdateScheduleForJob(user, job, jobViewModel);
                }

                await UpdateJobObjects(job, jobViewModel);

                await _jobObjectRepository.SaveChangesAsync();

                await _jobRepository.SaveChangesAsync();
            }

            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveJobs(string jobIds)
        {
            if (string.IsNullOrEmpty(jobIds))
            {
                return NoContent();
            }

            var ids = JsonConvert.DeserializeObject<List<int>>(jobIds) ?? new List<int>();

            var user = await CheckUserPermissions(Permissions.Write);

            var jobs = (await _jobRepository.FindAsync(f => ids.Contains(f.Id), null, null,
                    i => i.Include(p => p.JobObjects))).ToList();

            foreach (var job in jobs)
            {
                if (job == null)
                    return NotFound("Job with the this id doesn't exist.");

                if (job.Status == JobStatus.Running)
                    throw new DeleteBackupJobException("Can not delete a running job");
            }

            await _jobRepository.SaveChangesAsync();

            foreach (var job in jobs)
            {
                _jobRepository.Remove(job);
            }

            await _jobRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("Cost")]
        public async Task<ActionResult<JobCostViewModel>> GetJobCost([FromBody] JobViewModel jobViewModel)
        {
            var settings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
            var options = JsonConvert.DeserializeObject<JobOptions>(jobViewModel.JobOptions!, settings);

            var priceServiceData = new CostJobInputData
            {
                JobObjects = jobViewModel!.JobObjects!.Select(x => new JobObjectInfo(x.GetJobObject())).ToList(),
                Schedule = jobViewModel!.Schedule!.GetSchedule(),
                ReplicationOptions = options!.ReplicationOptions,
                RetentionPolicy = options.RetentionPolicy
            };

            var costResult = await _costService.GetJobMonthlyCost(CurrentUser.Id, new List<CostJobInputData> { priceServiceData });

            var costViewModel = new JobCostViewModel(costResult!.FirstOrDefault()!);

            return Json(costViewModel);
        }

        private async Task UpdateJobObjects(Job job, JobViewModel jobViewModel)
        {
            if (job.JobObjects == null)
            {
                job.JobObjects = new List<JobObject>();
            }

            var detachedObjectIds = new List<int>();

            if (jobViewModel.JobObjects != null)
            {
                await AddNewJobObjects(job, jobViewModel.JobObjects);

                foreach (var jobObject in job.JobObjects)
                {
                    var obj = jobViewModel.JobObjects
                        .FirstOrDefault(o => o.ObjectId == jobObject.ObjectId && o.Type == jobObject.Type);

                    if (obj == null)
                    {
                        detachedObjectIds.Add(jobObject.Id);
                    }
                }
            }

            var detachedObjects = await _jobObjectRepository.FindByIdsAsync(detachedObjectIds.ToArray(), null);

            foreach (var detachedObject in detachedObjects)
            {
                _jobObjectRepository.Remove(detachedObject);

                if (detachedObject.Type == JobObjectType.Instance)
                {
                    var childObjects = job.JobObjects.Where(o => o.ParentId == detachedObject.ObjectId).ToList();

                    foreach (var childObject in childObjects)
                    {
                        _jobObjectRepository.Remove(childObject);
                    }
                }
            }
        }

        private async Task AddNewJobObjects(Job job, List<JobObjectViewModel>? jobViewModelObjects)
        {
            var profileIds = await _profileService.GetAllowedProfileIds(CurrentUser.Id);

            if (!profileIds.Any())
            {
                throw new ArgumentException($"Current user: {CurrentUser.Name} does not have access to profiles");
            }

            if (jobViewModelObjects != null)
            {
                foreach (var jobModel in jobViewModelObjects)
                {
                    if (job.JobObjects.FirstOrDefault(o => o.ObjectId == jobModel.ObjectId &&
                                                           o.ProfileId == jobModel.ProfileId) != null)
                    {
                        continue;
                    }

                    var jobObject = jobModel.GetJobObject(job.Id);

                    job.JobObjects.Add(jobObject);
                }
            }
        }

        private async Task UpdateScheduleForJob(User currentUser, Job job, JobViewModel jobViewModel)
        {
            if (currentUser == null)
                throw new ArgumentNullException(nameof(currentUser));

            // Get current and new schedule
            var schedule = jobViewModel.Schedule?.Id != 0
                ? await _scheduleService.GetScheduleAsync(currentUser.Id, jobViewModel.Schedule != null ? jobViewModel.Schedule.Id : 0)
                : new Schedule();
            var oldScheduleParams = schedule.Params;

            var updatedSchedule = jobViewModel.Schedule?.GetSchedule();

            if (updatedSchedule != null)
            {
                var updatedScheduleParams = _scheduleService.ConvertScheduleDatesToUtc(updatedSchedule, currentUser.UtcOffset);

                updatedSchedule.TenantId = job.TenantId;

                // One-time schedules are tied to job. If this schedule is changed => create new schedule
                if (updatedSchedule.StartupType != StartupType.Recurring && updatedScheduleParams != oldScheduleParams)
                {
                    updatedSchedule.Id = 0;
                }

                // Update or create new schedule
                if (updatedSchedule.Id == 0)
                {
                    updatedSchedule = await _scheduleService.AddScheduleAsync(currentUser.Id, updatedSchedule);
                }
                else
                {
                    if (jobViewModel.Schedule != null)
                    {
                        await _scheduleService.UpdateScheduleAsync(currentUser.Id, jobViewModel.Schedule.Id, updatedSchedule);
                    }
                }

                // Reset job's next run if schedule has changed.
                // Changes of job's schedule params are handled in _jobService.UpdateScheduleAsync()
                if (job.ScheduleId != updatedSchedule.Id)
                {
                    job.NextRunAt = null;
                }

                job.ScheduleId = updatedSchedule.Id;
            }
        }

        private static JobOptions GetJobOptions(string? config)
        {
            JobOptions options = null!;

            if (!string.IsNullOrEmpty(config))
            {
                try
                {
                    var settings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
                    options = JsonConvert.DeserializeObject<JobOptions>(config, settings) ?? new JobOptions();
                }
                catch
                {
                    options = new JobOptions();
                }
            }

            return options;
        }

        private async Task<User> CheckUserPermissions(Permissions requiredPermissions)
        {
            var userId = CurrentUser?.Id ?? throw new ArgumentException("Current user doesn't exist");

            var permissions = await _roleRepository.GetPermissionsByUserIdAsync(userId);

            if (!permissions.BackupRights.HasFlag(requiredPermissions))
                throw new UnauthorizedAccessException($"No '{requiredPermissions}' permission for backup jobs.");

            return CurrentUser;
        }

        private static JobViewModelList GetJobViewModelList(User currentUser, IEnumerable<Job> jobs, int totalCount)
        {
            var model = new JobViewModelList { TotalCount = totalCount };

            foreach (var job in jobs)
            {
                var jobViewModel = GetJobViewModel(currentUser, job);
                model.Items.Add(jobViewModel);
            }

            return model;
        }

        private static JobViewModel GetJobViewModel(User currentUser, Job job)
        {
            var offset = currentUser?.UtcOffset ?? TimeSpan.Zero;

            return new JobViewModel(job, offset);
        }
    }

    public class JobViewModelList : ModelList<JobViewModel>
    {
    }

    public class JobViewModel
    {
        /// <summary>
        /// Unique identifier. Must be "0" if new job created.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// System field, used to handle concurrency conflicts.
        /// </summary>
        public string? RowVersion { get; set; }

        /// <summary>
        /// Name of Backup Job, Displayed in web UI
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Type of backup job. "Backup" – backup job without replication. "Backup and Replication" – backup job with replications.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Identifier of tenant, please see api calls for tenants.
        /// </summary>
        public int? TenantId { get; set; }

        /// <summary>
        /// Name of tenant, please see api calls for tenants.
        /// </summary>
        public string? TenantName { get; set; }

        /// <summary>
        /// Description of backup job.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Identifier of user triggered backup.
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Last time when job triggered backup, not required. Date time format: MMM dd, yyyy hh:mm:ss tt
        /// </summary>
        public string? LastRunAt { get; set; }

        /// <summary>
        /// BackupJob status. 0 – Idle, 1 – Running, 2 – Stopped. Set "Idle" status for newly created job.
        /// </summary>
        public string? Status { get; set; }

        public ScheduleViewModel? Schedule { get; set; }
        public List<JobObjectViewModel>? JobObjects { get; set; }
        public int? ObjectCount { get; set; }
        public string? JobOptions { get; set; }

        public JobViewModel()
        {
            Id = 0;
        }

        public JobViewModel(Job job, TimeSpan utcOffset = new TimeSpan())
        {
            Id = job.Id;
            RowVersion = Convert.ToBase64String(job.RowVersion);
            Name = job.Name;
            TenantId = job.TenantId;
            TenantName = job.Tenant?.Name ?? string.Empty;
            Description = job.Description;
            UserId = job.UserId ?? 0;
            Status = job.Status.ToString();
            LastRunAt = job.LastRunAt.HasValue
                ? DateTimeHelper.FormatWithUtcOffset(job.LastRunAt.Value, utcOffset)
                : string.Empty;
            ObjectCount = job.ObjectCount ?? 0;
            Schedule = job.Schedule != null ? new ScheduleViewModel(job.Schedule) : null;
            JobObjects = job.JobObjects
                .EmptyIfNull()
                .Select(jobObject => new JobObjectViewModel(jobObject)).ToList();
            JobOptions = job.Configuration != null ? job.Configuration.Configuration : JsonConvert.SerializeObject(new JobOptions());
            Type = job.Type == JobType.BackupAndReplication ? "Backup and Replication" : "Backup";
        }
    }

    public class JobObjectViewModel
    {
        public string ObjectId { get; set; }
        public JobObjectType? Type { get; set; }
        public int? ProfileId { get; set; }
        public string FolderId { get; set; }

        public JobObjectViewModel()
        {
            ObjectId = string.Empty;
            FolderId = string.Empty;
        }

        public JobObject GetJobObject()
        {
            return new JobObject
            {
                ObjectId = ObjectId,
                Type = Type ?? JobObjectType.Instance,
                ProfileId = ProfileId ?? 0,
                FolderId = FolderId,
                CustomData = string.Empty,
                Region = string.Empty,
                ParentId = string.Empty
            };
        }

        public JobObject GetJobObject(int jobId)
        {
            var jobObject = GetJobObject();
            jobObject.JobId = jobId;

            return jobObject;
        }

        public JobObjectViewModel(JobObject jobObject)
        {
            ObjectId = jobObject.ObjectId;
            Type = jobObject.Type;
            ProfileId = jobObject.ProfileId;
            FolderId = jobObject.FolderId;
        }
    }

    public class JobCostViewModel
    {
        public decimal Cost { get; set; }
        public string Currency { get; set; }
        public JobObjectCostViewModel[] ObjectCosts { get; set; }

        public double DailyDataChangeRatio { get; set; }
        public double UsedDiskSpaceRatio { get; set; }
        public double CompressionRatio { get; set; }

        public JobCostViewModel(JobCost jobCost)
        {
            Cost = jobCost.CostDetails.Length > 0 ? Math.Round(jobCost.CostDetails.Sum(x => x.Cost.Value), 2) : 0;
            Currency = jobCost.CostDetails.Select(x => x.Cost.Currency).Distinct().SingleOrDefault() ?? "₽";
            ObjectCosts = jobCost.CostDetails
                .GroupBy(x => x.JobObjectType)
                .Select(g => new JobObjectCostViewModel
                {
                    JobObjectType = g.Key,
                    Details = g.Select(x => new JobObjectCostDetailsViewModel
                    {
                        Cost = Math.Round(x.Cost.Value, 2),
                        Currency = x.Cost.Currency,
                        CostDescription = x.OperationType.GetEnumDescription()
                    }).ToArray()
                })
                .OrderBy(x => x.JobObjectType)
                .ToArray();
            DailyDataChangeRatio = jobCost.DailyDataChangeRatio;
            UsedDiskSpaceRatio = jobCost.UsedDiskSpaceRatio;
            CompressionRatio = jobCost.CompressionRatio;
        }
    }

    public class JobObjectCostViewModel
    {
        public JobObjectType JobObjectType { get; set; }
        public JobObjectCostDetailsViewModel[]? Details { get; set; }
    }

    public class JobObjectCostDetailsViewModel
    {
        public string? CostDescription { get; set; }
        public decimal Cost { get; set; }
        public string? Currency { get; set; }
    }
}
