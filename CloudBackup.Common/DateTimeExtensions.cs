using System.Globalization;

namespace CloudBackup.Common
{
    public static class DateTimeExtensions
    {
        private static readonly DateTime OldMonday = new(1900, 01, 01);
        
        public static string GetDayOfWeekName(this DayOfWeek dayOfWeek, CultureInfo? cultureInfo = null)
        {
            return (cultureInfo ?? CultureInfo.CurrentUICulture).DateTimeFormat.GetDayName(dayOfWeek);
        }

        public static DateTime ChangeTime(this DateTime dateTime, TimeSpan time)
        {
            if (time < TimeSpan.Zero || time > TimeSpan.FromDays(1))
                throw new ArgumentException("Time must be less than a day.");

            return dateTime.Date + time;
        }
        
        /// <summary>
        /// Returns same date but with another year.
        /// </summary>
        /// <param name="dt">Date.</param>
        /// <param name="year">New year value.</param>
        /// <returns>Same date but with <see cref="year"/> year.</returns>
        public static DateTime ChangeYear(this DateTime dt, int year)
        {
            if (!DateTime.IsLeapYear(year) && dt.Month == 02 && dt.Day == 29)
            {
                return new DateTime(year, 02, 28, dt.Hour, dt.Minute, dt.Second);
            }

            return new DateTime(year, dt.Month, dt.Day);
        }

        /// <summary>
        /// Get end of date for a date.
        /// </summary>
        /// <param name="date">Date.</param>
        public static DateTime EndOfDay(this DateTime date)
        {
            return date.Date.Add(new TimeSpan(0, 23, 59, 59, 999));
        }

        public static DateTime BeginOfWeek(this DateTime date)
        {
            return date.AddDays(-1 * ((date - OldMonday).Days % 7)).Date;
        }

        public static DateTime EndOfWeek(this DateTime date)
        {
            return date.AddDays(-1 * ((date - OldMonday).Days % 7) + 6).EndOfDay();
        }

        public static DateTime BeginOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        public static DateTime EndOfMonth(this DateTime date)
        {
            return date.AddDays(DateTime.DaysInMonth(date.Year, date.Month) - date.Day).EndOfDay();
        }

        public static DateTime Min(this DateTime left, DateTime right)
        {
            return left <= right ? left : right;
        }

        public static DateTime Max(this DateTime left, DateTime right)
        {
            return left >= right ? left : right;
        }

        public static DateTime AddWeeks(this DateTime dt, int weeks)
        {
            return dt.AddDays(7 * weeks);
        }

        public static bool Between(this DateTime date, DateTime from, DateTime to)
        {
            return date >= from && date <= to;
        }

        public static int MonthDiff(this DateTime from, DateTime to)
        {
            return (to.Year - from.Year) * 12 + to.Month - from.Month;
        }

        /// <summary>
        /// Returns the last date with the specified day of week before the input date.
        /// </summary>
        public static DateTime GetLastDayOfWeekDate(this DateTime date, DayOfWeek dayOfWeek)
        {
            int daysToAdd = ((int) dayOfWeek - (int) date.DayOfWeek - 7) % 7;
            return date.AddDays(daysToAdd);
        }

        /// <summary>
        /// Returns the next date with the specified day of week after the input date.
        /// </summary>
        public static DateTime GetNextDayOfWeekDate(this DateTime date, DayOfWeek dayOfWeek)
        {
            int daysToAdd = ((int) dayOfWeek - (int) date.DayOfWeek + 7) % 7;
            return date.AddDays(daysToAdd);
        }

        public static double TotalWeeks(this TimeSpan timeSpan)
        {
            return timeSpan.TotalDays / 7;
        }

        public static TimeSpan Min(this TimeSpan left, TimeSpan right)
        {
            return left <= right ? left : right;
        }

        public static TimeSpan Max(this TimeSpan left, TimeSpan right)
        {
            return left >= right ? left : right;
        }

        public static bool Between(this TimeSpan time, DateTime from, DateTime to)
        {
            return from.ChangeTime(time).Between(from, to) || to.ChangeTime(time).Between(from, to) || (from - to).Days > 0;
        }

        public static TimeSpan AddTime(this TimeSpan time, TimeSpan timeToAdd)
        {
            return DateTime.Today.Add(time + timeToAdd).TimeOfDay;
        }

        public static long GetUnixTicks(this DateTime source)
        {
            return (long)source.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, source.Kind)).TotalMilliseconds;
        }

        public static DateTime GetDateTimeFromUnixTicks(long millisecons)
        {
            return new DateTime(10000 * millisecons + (new DateTime(1970, 1, 1, 0, 0, 0)).Ticks);
        }

        public static DateTime GetDateTimeFromUnixTicks(long millisecons, DateTimeKind kind)
        {
            return new DateTime(10000 * millisecons + (new DateTime(1970, 1, 1, 0, 0, 0)).Ticks, kind);
        }
    }
}