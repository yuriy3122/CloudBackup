using Microsoft.AspNetCore.Mvc;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Services;
using CloudBackup.Common;

namespace CloudBackup.Management.Controllers
{
    [Route("Core/Schedule")]
    public class ScheduleController : CommonController
    {
        private readonly IScheduleService _scheduleService;

        public ScheduleController(IUserRepository userRepository, IScheduleService scheduleService
            ) : base(userRepository)
        {
           _scheduleService = scheduleService;
        }

        [HttpGet]
        public async Task<ActionResult<ModelList<ScheduleViewModel>>> GetSchedules(int? tenantId, StartupType? startupType, 
            int? pageSize, int? pageNum, string order, string filter)
        {
            var options = new QueryOptions<Schedule>(new EntitiesPage(pageSize ?? short.MaxValue, pageNum ?? 1), order, filter);
            var schedules = await _scheduleService.GetSchedulesAsync(CurrentUser.Id, tenantId, startupType, options);

            var scheduleViewModels = schedules.Items.Select(schedule => new ScheduleViewModel(schedule, CurrentUser.UtcOffset));
            var scheduleViewModelList = ModelList.Create(scheduleViewModels, schedules.TotalCount);

            return scheduleViewModelList;
        }

        [HttpGet]
        [Route("{scheduleId:int}")]
        public async Task<ActionResult<ScheduleViewModel>> GetSchedule(int scheduleId)
        {
            var schedule = await _scheduleService.GetScheduleAsync(CurrentUser.Id, scheduleId);

            var scheduleViewModel = new ScheduleViewModel(schedule, CurrentUser.UtcOffset);

            return scheduleViewModel;
        }

        [HttpPost]
        public async Task<ActionResult<ScheduleViewModel>> AddSchedule([FromBody] ScheduleViewModel scheduleViewModel)
        {
            if (scheduleViewModel == null)
            {
                throw new ArgumentNullException(nameof(scheduleViewModel));
            }

            var schedule = scheduleViewModel.GetSchedule();

            var newSchedule = await _scheduleService.AddScheduleAsync(CurrentUser.Id, schedule);

            var newScheduleViewModel = new ScheduleViewModel(newSchedule, CurrentUser.UtcOffset);

            return CreatedAtAction(nameof(GetSchedule), new { scheduleId = newSchedule.Id }, newScheduleViewModel);
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleViewModel scheduleViewModel)
        {
            if (scheduleViewModel == null)
            {
                throw new ArgumentNullException(nameof(scheduleViewModel));
            }

            var schedule = scheduleViewModel.GetSchedule();

            await _scheduleService.UpdateScheduleAsync(CurrentUser.Id, id, schedule);

            return Ok();
        }

        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            await _scheduleService.DeleteScheduleAsync(CurrentUser.Id, id);

            return Ok();
        }
    }

    public class ScheduleViewModel
    {
        public int Id { get; set; }
        public string? RowVersion { get; set; }
        public string? Name { get; set; }
        public int? TenantId { get; set; }
        public TenantViewModel? Tenant { get; set; }
        public StartupType? StartupType { get; set; }
        public OccurType OccurType { get; set; }
        public DateTime Created { get; set; }
        public string? CreatedText => DateTimeHelper.Format(Created);
        public int? JobCount { get; set; }

        /// <summary>
        /// Params in JSON format, depends on StartupType and OccurType. Example: {"time": "03:00:00.000"}
        /// </summary>
        public string? Params { get; set; }

        public string? ParamsDescription { get; set; }

        public ScheduleViewModel()
        {
        }

        public ScheduleViewModel(Schedule schedule, TimeSpan utcOffset = new TimeSpan())
        {
            Id = schedule.Id;
            RowVersion = Convert.ToBase64String(schedule.RowVersion);
            Name = schedule.Name;
            TenantId = schedule.TenantId;
            Tenant = schedule.Tenant != null ? new TenantViewModel(schedule.Tenant) : new TenantViewModel();
            StartupType = schedule.StartupType;
            OccurType = schedule.OccurType ?? OccurType.Unknown;
            Params = ScheduleParamConverter.ConvertScheduleDatesToUserTimeZone(schedule, utcOffset);
            Created = schedule.Created.Add(utcOffset);
            JobCount = schedule.JobCount;

            var oldParams = schedule.Params;
            schedule.Params = Params;
            var param = ScheduleParamFactory.CreateScheduleParam(schedule);
            schedule.Params = oldParams;

            if (param != null)
            {
                ParamsDescription = param.GetDescription();
            }
        }

        public Schedule GetSchedule()
        {
            return new Schedule
            {
                Id = Id,
                Name = Name ?? string.Empty,
                OccurType = OccurType,
                StartupType = StartupType ?? Model.StartupType.Immediately,
                TenantId = TenantId ?? 0,
                Tenant = Tenant?.GetTenant() ?? new Tenant(),
                Params = Params ?? string.Empty
            };
        }
    }
}