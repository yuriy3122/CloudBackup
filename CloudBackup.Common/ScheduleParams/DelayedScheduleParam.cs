using Newtonsoft.Json;

namespace CloudBackup.Common.ScheduleParams
{
    public class DelayedScheduleParam : IScheduleParam
    {
        public RecurringPeriodType RecurringPeriodType => RecurringPeriodType.None;

        public DateTime RunAtDateTime { get; set; }

        public string ConvertDatesToUtc(TimeSpan utcOffset)
        {
            DelayedScheduleParam param = new()
            {
                RunAtDateTime = RunAtDateTime.Subtract(utcOffset)
            };
            return JsonConvert.SerializeObject(param);
        }

        public string FormatDatesToUserTimeZone(TimeSpan utcOffset)
        {
            DelayedScheduleParam param = new DelayedScheduleParam()
            {
                RunAtDateTime = RunAtDateTime.Add(utcOffset)
            };
            return JsonConvert.SerializeObject(param);
        }

        public DateTime? GetScheduledDateTime(DateTime? datetime)
        {
            return RunAtDateTime;
        }

        public DateTime GetNextRun(DateTime prevRun)
        {
            return prevRun <= RunAtDateTime ? RunAtDateTime : DateTime.MaxValue;
        }

        public bool IsPermittedToRunNow()
        {
            return true;
        }

        public string GetDescription()
        {
            return $"Run {RunAtDateTime:f}";
        }
    }
}
