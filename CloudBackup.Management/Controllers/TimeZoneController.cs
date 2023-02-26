using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CloudBackup.Common;
using CloudBackup.Services;
using CloudBackup.Repositories;

namespace CloudBackup.Management.Controllers
{
    [Route("Core")]
    public class TimeZoneController : Controller
    {
        private readonly ITimeZoneService _timeZoneService;

        public TimeZoneController(ITimeZoneService timeZoneService)
        {
            _timeZoneService = timeZoneService;
        }

        [AllowAnonymous]
        [HttpGet("TimeZones")]
        public ActionResult<ModelList<TimeZoneViewModel>> GetTimeZones()
        {
            var timeZones = _timeZoneService.GetTimeZones().Select(x => new TimeZoneViewModel(x)).ToList();
            var viewModelList = ModelList.Create(timeZones, timeZones.Count);

            return viewModelList;
        }

        [HttpGet("{userId:int}/TimeZone")]
        public async Task<ActionResult<TimeZoneViewModel>> GetUserTimeZone(int userId)
        {
            var offset = await _timeZoneService.GetUserUtcOffsetAsync(userId);
            var timeZone = new TimeZoneViewModel(offset);

            return timeZone;
        }

        [AllowAnonymous]
        [HttpGet("UtcOffsets")]
        public ActionResult<List<UtcOffsetViewModel>> GetUtcOffsets()
        {
            var result = _timeZoneService.GetTimeZones()
                .Select(x => x.BaseUtcOffset)
                .Distinct()
                .Select(offset => new UtcOffsetViewModel(offset))
                .ToList();

            return result;
        }

        [HttpGet("{userId:int}/UtcOffset")]
        public async Task<ActionResult<UtcOffsetViewModel>> GetUserUtcOffset(int userId)
        {
            var offset = await _timeZoneService.GetUserUtcOffsetAsync(userId);

            return new UtcOffsetViewModel(offset);
        }
    }

    public class TimeZoneViewModel
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public TimeSpan UtcOffset { get; set; }

        public TimeZoneViewModel()
        {
        }

        public TimeZoneViewModel(TimeSpan utcOffset) : this(DateTimeHelper.GetTimeZoneInfo(utcOffset))
        {
        }

        public TimeZoneViewModel(TimeZoneInfo timeZoneInfo)
        {
            Id = timeZoneInfo.Id;
            Name = timeZoneInfo.DisplayName;
            ShortName = timeZoneInfo.BaseUtcOffset.TotalHours.ToString("UTC+0.#;UTC-#.#");
            UtcOffset = timeZoneInfo.BaseUtcOffset;
        }
    }

    public class UtcOffsetViewModel
    {
        public TimeSpan Offset { get; set; }

        public string Name => string.Format("UTC{0:+00;-00}:{1:00}", Offset.Hours, Math.Abs(Offset.Minutes));

        public UtcOffsetViewModel()
        {
        }

        public UtcOffsetViewModel(TimeSpan offset)
        {
            Offset = offset;
        }
    }
}