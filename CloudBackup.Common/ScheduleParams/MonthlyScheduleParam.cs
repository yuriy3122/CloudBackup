using System.Globalization;
using Newtonsoft.Json;

namespace CloudBackup.Common.ScheduleParams
{
    //JSON: {"DayOfMonth":5,"TimeOfDay":"18:30:00.0000000","MonthList":[2,3,4,6]}

    public class MonthlyScheduleParam : IScheduleParam
    {
        public const int LastDayOfMonth = -1;

        public RecurringPeriodType RecurringPeriodType => RecurringPeriodType.Year;

        public MonthlyScheduleParam()
        {
            MonthList = new List<int>();
        }

        /// <summary>
        /// Day of month. "-1" means the last day of month.
        /// </summary>
        public int DayOfMonth { get; set; }

        public TimeSpan TimeOfDay { get; set; }

        /// <summary>
        /// List of months. The month component, expressed as a value between 1 and 12.
        /// </summary>
        public List<int> MonthList { get; set; }

        public string ConvertDatesToUtc(TimeSpan utcOffset)
        {
            var param = new MonthlyScheduleParam
            {
                MonthList = MonthList,
                DayOfMonth = DayOfMonth,
                TimeOfDay = TimeOfDay.Subtract(utcOffset),
            };
            param.FixTimeOffset(true);

            return JsonConvert.SerializeObject(param);
        }

        public string FormatDatesToUserTimeZone(TimeSpan utcOffset)
        {
            var param = new MonthlyScheduleParam
            {
                MonthList = MonthList,
                DayOfMonth = DayOfMonth,
                TimeOfDay = TimeOfDay.Add(utcOffset),
            };
            param.FixTimeOffset(false);

            return JsonConvert.SerializeObject(param);
        }

        public DateTime? GetScheduledDateTime(DateTime? datetime)
        {
            var date = GetNext(datetime);

            if (date < DateTime.UtcNow)
            {
                date = GetNext(date); // next month
            }

            return date;
        }

        private DateTime GetNext(DateTime? datetime)
        {
            var date = DateTime.UtcNow.BeginOfMonth();
            date = datetime.HasValue ? date.AddMonths(1) : date;

            MonthList.Sort();

            int year = date.Year;
            int month = MonthList.FirstOrDefault(m => m >= date.Month);

            if (month == 0) // No more executions in current year
            {
                if (MonthList.Count > 0)
                {
                    month = MonthList.First();
                    year++;
                }
                else
                {
                    month = 1;
                }
            }

            var lastDayInMonth = DateTime.DaysInMonth(year, month);

            if (DayOfMonth > lastDayInMonth)
            {
                return DateTime.UtcNow.AddMonths(1).BeginOfMonth();
            }

            var day = DayOfMonth;

            if (DayOfMonth < 0)
            {
                day = lastDayInMonth;
            }

            var dateTime = new DateTime(year, month, day);
            dateTime = dateTime.AddHours(TimeOfDay.Hours);
            dateTime = dateTime.AddMinutes(TimeOfDay.Minutes);
            dateTime = dateTime.AddSeconds(TimeOfDay.Seconds);

            return dateTime;
        }

        public DateTime GetNextRun(DateTime prevRun)
        {
            for (int addMonths = 0; addMonths <= 12; addMonths++)
            {
                // add months
                if (prevRun.MonthDiff(DateTime.MaxValue) < addMonths)
                    return DateTime.MaxValue;

                var nextRun = prevRun.BeginOfMonth().AddMonths(addMonths);

                if (!MonthList.Contains(nextRun.Month))
                    continue;

                // get day of month
                var daysInMonth = DateTime.DaysInMonth(nextRun.Year, nextRun.Month);
                var dayOfMonth = DayOfMonth == LastDayOfMonth ? daysInMonth : DayOfMonth;

                if (dayOfMonth > daysInMonth)
                    continue;

                // add day and time
                var addTime = TimeSpan.FromDays(dayOfMonth - 1) + TimeOfDay;
                if (DateTime.MaxValue - nextRun < addTime)
                    return DateTime.MaxValue;

                nextRun = nextRun.Add(addTime);

                if (nextRun > prevRun)
                    return nextRun;
            }

            return DateTime.MaxValue;
        }

        public bool IsPermittedToRunNow()
        {
            var now = DateTime.UtcNow;

            foreach (var month in MonthList)
            {
                // TimeOfDay can be <0:00 or >24:00, so it's important to construct date add add TimeOfDay to it.
                var daysInMonth = DateTime.DaysInMonth(now.Year, month);
                var day = DayOfMonth == LastDayOfMonth ? daysInMonth : DayOfMonth;

                if (day <= daysInMonth)
                {
                    var dateToCheck = new DateTime(now.Year, month, day).Add(TimeOfDay);

                    if (now.Month == dateToCheck.Month && now.Day == dateToCheck.Day && now.TimeOfDay > dateToCheck.TimeOfDay)
                        return true;
                }
            }

            return false;
        }

        public string GetDescription()
        {
            var dayText = DayOfMonth == LastDayOfMonth ? "last day" : DayOfMonth + DateTimeHelper.GetDaySuffix(DayOfMonth);
            var timeText = DateTime.UtcNow.Date.Add(TimeOfDay).ToString("hh:mm tt", CultureInfo.InvariantCulture);
            
            string monthText;
            switch (MonthList.Count)
            {
                case 1:
                    monthText = DateTimeFormatInfo.InvariantInfo.GetMonthName(MonthList.First());
                    break;
                case 9:
                case 10:
                case 11:
                    var excludedMonths = Enumerable.Range(1, 12)
                        .Except(MonthList)
                        .Select(month => DateTimeFormatInfo.InvariantInfo.GetMonthName(month));
                    monthText = "every month except " + string.Join(", ", excludedMonths);
                    break;
                case 12:
                    monthText = "every month";
                    break;
                default:
                    var monthRanges = MonthList
                        .Distinct()
                        .OrderBy(x => x)
                        .GroupAdjacentBy((month1, month2) =>
                        {
                            var monthsDiff = Math.Abs(month1 - month2);
                            return monthsDiff == 1 || monthsDiff == 12;
                        })
                        .Select(months => new {Min = months.Min(), Max = months.Max()});

                    monthText = string.Join(", ", monthRanges.Select(x =>
                        DateTimeFormatInfo.InvariantInfo.GetAbbreviatedMonthName(x.Min) +
                        (x.Min == x.Max ? string.Empty : $"-{DateTimeFormatInfo.InvariantInfo.GetAbbreviatedMonthName(x.Max)}")));
                    break;
            }

            return $"Run at {timeText} on {dayText} of {monthText}";
        }

        /// <summary>
        /// Correct params if <see cref="TimeOfDay"/> is out of range after applying UTC offset.
        /// If <see cref="utc"/> is true, params won't be corrected if they are not reversible.
        /// </summary>
        private void FixTimeOffset(bool utc)
        {
            // TODO find a way to handle all cases
            // Currently, TimeOfDay can be less than zero or more than 24:00.
            // This happens when it's not possible to shift days because this operation won't be reversible
            // or cannot be expressed with current params structure (examples can be found in code below).
            //
            // But when converting params to user time zone (utc == false), we need to show correct params.
            // Since some fixes are irreversible, this can lead to different results
            // when user with another time zone modifies a schedule (even when he didn't change anything!).
            // Specifically, the days aren't shifted and saving changes saves them for new user's offset.
            // This is a known issue and currently it's unclear how to handle it.
            var daysDiff = 0;
            if (TimeOfDay.Ticks >= TimeSpan.TicksPerDay)
            {
                // This leads to DayOfMonth == 1, but after reverse this leads to DayOfMonth == LastDayOfMonth,
                // which is not equal to initial result.
                if (utc && MonthList.Any(month => DayOfMonth == DateTime.DaysInMonth(1999, month)))
                    return;

                daysDiff = (int)(TimeOfDay.Ticks / TimeSpan.TicksPerDay);
            }
            else if (TimeOfDay < TimeSpan.Zero)
            {
                // DayOfMonth == LastDayOfMonth: resulting DayOfMonth is unknown, because it depends on a month.
                // DayOfMonth == DaysInMonth + 1: decreasing DayOfMonth might "enable" additional months with less days in month,
                //    which leads to different results.
                if (utc && (DayOfMonth == LastDayOfMonth || MonthList.Any(month => DayOfMonth == DateTime.DaysInMonth(2000, month) + 1)))
                    return;

                daysDiff = -1;
            }

            if (daysDiff != 0)
            {
                TimeOfDay -= TimeSpan.FromDays(daysDiff);

                var monthDiff = 0;

                if (DayOfMonth == LastDayOfMonth)
                {
                    if (daysDiff > 0)
                    {
                        DayOfMonth = daysDiff;
                        monthDiff = 1;
                    }

                    // daysDiff < 0 => do nothing
                }
                else if (DayOfMonth == 1 && daysDiff == -1)
                {
                    DayOfMonth = -1;
                    monthDiff = -1;
                }
                else
                {
                    DayOfMonth += daysDiff;
                    monthDiff = (DayOfMonth - 1) / 31;
                    DayOfMonth = (DayOfMonth - 1).Modulus(31) + 1;
                }

                if (monthDiff != 0)
                {
                    for (int i = 0; i < MonthList.Count; i++)
                    {
                        MonthList[i] = (MonthList[i] - 1 + monthDiff).Modulus(12) + 1;
                    }
                }
            }
        }
    }
}