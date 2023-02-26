using Newtonsoft.Json;
using System.Globalization;

namespace CloudBackup.Common.ScheduleParams
{
    //JSON: {"Days":[0,1,2,3,4,5,6],"Time":"18:15:00.0000000"}

    public class DailyScheduleParam : IScheduleParam
    {
        public DailyScheduleParam()
        {
            Days = new List<DayOfWeek>();
        }

        public RecurringPeriodType RecurringPeriodType => RecurringPeriodType.Week;

        public List<DayOfWeek> Days { get; set; }

        public TimeSpan Time { get; set; }

        public string ConvertDatesToUtc(TimeSpan utcOffset)
        {
            DailyScheduleParam param = new()
            {
                Time = Time.Subtract(utcOffset),
                Days = Days
            };
            param.FixTimeOffset();

            return JsonConvert.SerializeObject(param);
        }

        public string FormatDatesToUserTimeZone(TimeSpan utcOffset)
        {
            DailyScheduleParam param = new DailyScheduleParam()
            {
                Time = Time.Add(utcOffset),
                Days = Days.ToList()
            };
            param.FixTimeOffset();

            return JsonConvert.SerializeObject(param);
        }

        public DateTime? GetScheduledDateTime(DateTime? datetime)
        {
            var date = GetNext(datetime);

            if (date < DateTime.UtcNow)
            {
                date = GetNext(date); // next day
            }

            return date;
        }

        private DateTime GetNext(DateTime? datetime)
        {
            var dateTime = datetime.HasValue ? DateTime.UtcNow.AddDays(1) : DateTime.UtcNow;
            var date = dateTime.Date;
            date = date.AddHours(Time.Hours);
            date = date.AddMinutes(Time.Minutes);
            date = date.AddSeconds(Time.Seconds);

            return date;
        }

        public DateTime GetNextRun(DateTime prevRun)
        {
            for (int addDays = 0; addDays <= 7; addDays++)
            {
                var addTime = TimeSpan.FromDays(addDays).Add(Time);

                if (DateTime.MaxValue - prevRun.Date < addTime)
                    return DateTime.MaxValue;

                var nextRun = prevRun.Date.Add(addTime);

                if (!Days.Contains(nextRun.DayOfWeek))
                    continue;

                if (nextRun > prevRun)
                    return nextRun;
            }

            return DateTime.MaxValue;
        }

        public bool IsPermittedToRunNow()
        {
            var now = DateTime.UtcNow;

            var dayMatches = Days.Contains(now.DayOfWeek);

            if (dayMatches && now.TimeOfDay > Time)
            {
                return true;
            }

            return false;
        }

        public string GetDescription()
        {
            var timeText = DateTime.UtcNow.Date.Add(Time).ToString("hh:mm tt", CultureInfo.InvariantCulture);

            string daysText;
            switch (Days.Count)
            {
                case 0:
                    daysText = "on none of the days";
                    break;
                case 1:
                    daysText = "on " + DateTimeFormatInfo.InvariantInfo.GetDayName(Days.First());
                    break;
                case 5:
                case 6:
                    var excludedWeekDays = EnumHelper.GetValues<DayOfWeek>()
                        .Except(Days)
                        .Select(day => DateTimeFormatInfo.InvariantInfo.GetDayName(day));
                    daysText = "on every day except " + string.Join(", ", excludedWeekDays);
                    break;
                case 7:
                    daysText = "daily";
                    break;
                default:
                    var weekDayRanges = Days
                        .Distinct()
                        .OrderBy(x => x)
                        .GroupAdjacentBy((day1, day2) =>
                        {
                            var daysDiff = Math.Abs(day1 - day2);
                            return daysDiff == 1 || daysDiff == 6;
                        })
                        .Select(days => new {Min = days.Min(), Max = days.Max()});
                    daysText = "on " + string.Join(", ", weekDayRanges.Select(x =>
                    {
                        if (x.Max - x.Min == 1)
                            return x.Min + ", " + x.Max;

                        return DateTimeFormatInfo.InvariantInfo.GetAbbreviatedDayName(x.Min) +
                               (x.Min == x.Max ? string.Empty : $"-{DateTimeFormatInfo.InvariantInfo.GetAbbreviatedDayName(x.Max)}");
                    }));
                    break;
            }

            return $"Run at {timeText} {daysText}";
        }

        private void FixTimeOffset()
        {
            long daysDiff = 0;
            if (Time.Duration().Ticks >= TimeSpan.TicksPerDay)
            {
                daysDiff = Time.Ticks / TimeSpan.TicksPerDay;
            }
            else if (Time < TimeSpan.Zero)
            {
                daysDiff = -1;
            }

            if (daysDiff != 0)
            {
                Time -= TimeSpan.FromDays(daysDiff);

                for (int i = 0; i < Days.Count; i++)
                {
                    Days[i] = (DayOfWeek)((long)Days[i] + daysDiff).Modulus(7);
                }
            }
        }
    }
}