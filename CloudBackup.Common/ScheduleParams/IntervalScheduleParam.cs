using Newtonsoft.Json;
using System.Text;

namespace CloudBackup.Common.ScheduleParams
{
    //JSON: {"DailyIntervals":{"Monday":[{"Begin":"17:14:55.0000000","End":"19:14:55.0000000"},
    //                                   {"Begin":"21:14:55.0000000","End":"23:14:55.0000000"}],
    //                       "Thursday":[{"Begin":"17:14:55.0000000","End":"19:14:55.0000000"},
    //                                   {"Begin":"21:14:55.0000000","End":"23:14:55.0000000"}]},
    //       "TimeIntervalType":0,"TimeIntervalValue":4}

    public class IntervalScheduleParam : IScheduleParam
    {
        public RecurringPeriodType RecurringPeriodType => RecurringPeriodType.Unknown;

        public IntervalScheduleParam()
        {
            DailyIntervals = new DailyIntervals();
        }

        public DailyIntervals DailyIntervals { get; set; }

        public TimeIntervalType TimeIntervalType { get; set; }

        public int TimeIntervalValue { get; set; }

        public string ConvertDatesToUtc(TimeSpan utcOffset)
        {
            var param = new IntervalScheduleParam
            {
                DailyIntervals = DailyIntervals.AddOffset(-utcOffset),
                TimeIntervalType = TimeIntervalType,
                TimeIntervalValue = TimeIntervalValue,
            };

            return JsonConvert.SerializeObject(param);
        }

        public string FormatDatesToUserTimeZone(TimeSpan utcOffset)
        {
            var param = new IntervalScheduleParam
            {
                DailyIntervals = DailyIntervals.AddOffset(utcOffset),
                TimeIntervalType = TimeIntervalType,
                TimeIntervalValue = TimeIntervalValue
            };

            return JsonConvert.SerializeObject(param);
        }

        public DateTime? GetScheduledDateTime(DateTime? datetime)
        {
            var now = DateTime.UtcNow;

            if (datetime.HasValue)
            {
                if (TimeIntervalType == TimeIntervalType.Hour)
                {
                    now = now.AddHours(TimeIntervalValue);
                }
                else if (TimeIntervalType == TimeIntervalType.Minute)
                {
                    now = now.AddMinutes(TimeIntervalValue);
                }
            }

            return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
        }

        public DateTime GetNextRun(DateTime prevRun)
        {
            long intervalTicks;
            switch (TimeIntervalType)
            {
                case TimeIntervalType.Hour:
                    intervalTicks = TimeSpan.TicksPerHour;
                    break;
                case TimeIntervalType.Minute:
                    intervalTicks = TimeSpan.TicksPerMinute;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (DateTime.MaxValue - prevRun < TimeSpan.FromTicks(intervalTicks))
                return DateTime.MaxValue;
            
            var nextRun = prevRun.AddTicks(intervalTicks * TimeIntervalValue);
            if (!DailyIntervals.IsEmpty && !DailyIntervals.ContainsDate(nextRun))
            {
                // Next run is forbidden, so job will run in next available interval:
                var firstDateInInterval = DailyIntervals.Intervals
                    .SelectMany(x => x.Value.Select(interval => new { DayOfWeek = x.Key, Interval = interval }))
                    .Select(x =>
                    {
                        // Intervals earlier this week will happen only on next week
                        var beginInterval = nextRun.BeginOfWeek().GetNextDayOfWeekDate(x.DayOfWeek).Add(x.Interval.Begin);
                        return beginInterval <= nextRun ? beginInterval.AddDays(7) : beginInterval;
                    })
                    .OrderBy(date => date)
                    .FirstOrDefault();

                nextRun = firstDateInInterval != DateTime.MinValue ? firstDateInInterval : DateTime.MaxValue;
            }

            return nextRun;
        }

        public bool IsPermittedToRunNow()
        {
            if (DailyIntervals.IsEmpty)
                return true;

            return DailyIntervals.ContainsDate(DateTime.UtcNow);
        }

        public string GetDescription()
        {
            var intervalText = new StringBuilder();
            intervalText.Append(TimeIntervalType.ToString().ToLowerInvariant());
            if (TimeIntervalValue > 1)
            {
                intervalText.Insert(0, TimeIntervalValue + " ");
                intervalText.Append("s");
            }

            if (DailyIntervals.Intervals.SelectMany(x => x.Value).Any())
                intervalText.Append(" at selected intervals");

            return $"Run every {intervalText}";
        }
    }

    public class DailyIntervals
    {
        public Dictionary<DayOfWeek, List<Interval>> Intervals { get; set; }

        public int OffsetMinutes { get; set; }

        public bool IsEmpty => !Intervals.Any(x => x.Value.Count > 0);

        public DailyIntervals()
        {
            Intervals = new Dictionary<DayOfWeek, List<Interval>>();
        }

        public bool ContainsDate(DateTime dateTime)
        {
            var offset = TimeSpan.FromMinutes(OffsetMinutes);
            return Intervals.TryGetValue(dateTime.Date.DayOfWeek, out var intervals) &&
                   intervals.Any(x => x.Contains(dateTime.TimeOfDay - offset));
        }

        public DailyIntervals Clone()
        {
            return new DailyIntervals
            {
                Intervals = Intervals.ToDictionary(
                    x => x.Key,
                    x => x.Value
                        .Select(interval => new Interval(interval.Begin, interval.End))
                        .ToList()),
                OffsetMinutes = OffsetMinutes
            };
        }

        public DailyIntervals AddOffset(TimeSpan utcOffset)
        {
            var dailyIntervals = Clone();

            var offsetHours = TimeSpan.FromHours(utcOffset.Hours);

            foreach (var interval in dailyIntervals.Intervals.SelectMany(x => x.Value))
            {
                interval.Begin = interval.Begin.Add(offsetHours);
                interval.End = interval.End.Add(offsetHours);
            }

            dailyIntervals.OffsetMinutes += utcOffset.Minutes;

            dailyIntervals.FixTimeOffset();

            return dailyIntervals;
        }

        public DailyIntervals ClearOffset()
        {
            return AddOffset(TimeSpan.FromMinutes(-OffsetMinutes));
        }

        private void FixTimeOffset()
        {
            if (OffsetMinutes < 0 || OffsetMinutes >= 60)
            {
                var hoursDiff = 0;
                if (OffsetMinutes >= 60)
                {
                    hoursDiff = OffsetMinutes / 60;
                }
                else if (OffsetMinutes < 0)
                {
                    hoursDiff = OffsetMinutes / 60 - 1;
                }

                var offset = TimeSpan.FromHours(hoursDiff);
                foreach (var interval in Intervals.SelectMany(x => x.Value))
                {
                    interval.Begin += offset;
                    interval.End += offset;
                }

                OffsetMinutes -= 60 * hoursDiff;
            }

            Intervals = Intervals
                .SelectMany(x => x.Value
                    .SelectMany(SplitIntervalByDays)
                    .Select(interval => new
                    {
                        WeekDay = x.Key,
                        Interval = interval
                    }))
                .Select(dailyInterval =>
                {
                    var weekDay = dailyInterval.WeekDay;
                    var begin = dailyInterval.Interval.Begin;
                    var end = dailyInterval.Interval.End;

                    // days shift
                    long daysDiff = 0;
                    if (end.Duration().Ticks > TimeSpan.TicksPerDay)
                    {
                        daysDiff = end.Ticks / TimeSpan.TicksPerDay;
                    }
                    else if (begin < TimeSpan.Zero)
                    {
                        daysDiff = -1;
                    }

                    if (daysDiff != 0)
                    {
                        weekDay = (DayOfWeek) ((long) weekDay + daysDiff).Modulus(7);
                        begin -= TimeSpan.FromDays(daysDiff);
                        end -= TimeSpan.FromDays(daysDiff);
                    }

                    return new
                    {
                        Weekday = weekDay,
                        Interval = new Interval(begin, end)
                    };
                })
                .GroupBy(x => x.Weekday, x => x.Interval)
                .Select(g => new
                {
                    WeekDay = g.Key,
                    Intervals = g
                        .OrderBy(x => x.Begin)
                        .GroupAdjacentBy((x, y) => x.End == y.Begin || x.Begin == y.End)
                        .Select(intervals => new Interval(intervals.Min(x => x.Begin), intervals.Max(x => x.End)))
                        .ToList()
                })
                .ToDictionary(x => x.WeekDay, x => x.Intervals);
        }

        private static IEnumerable<Interval> SplitIntervalByDays(Interval interval)
        {
            var boundaries = new[] { TimeSpan.Zero, TimeSpan.FromHours(24) };
            foreach (var boundary in boundaries)
            {
                if (interval.Contains(boundary, true))
                {
                    yield return new Interval(interval.Begin, boundary);
                    yield return new Interval(boundary, interval.End);
                    yield break;
                }
            }

            yield return interval;
        }
    }

    public class Interval
    {
        public Interval(TimeSpan begin, TimeSpan end)
        {
            if (end < begin)
            {
                throw new ArgumentException("End param is less then Begin param");
            }

            Begin = begin;
            End = end;
        }

        public TimeSpan Begin { get; set; }

        public TimeSpan End { get; set; }
        
        public bool Contains(TimeSpan time, bool exclusive = false)
        {
            return exclusive
                ? time > Begin && time < End
                : time >= Begin && time <= End;
        }
    }

    public enum TimeIntervalType
    {
        Hour = 0,
        Minute = 1
    }
}