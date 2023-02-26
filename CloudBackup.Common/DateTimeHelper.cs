using System.Globalization;

namespace CloudBackup.Common
{
    public static class DateTimeHelper
    {
        public static string Format(DateTime dateTime)
        {
            return dateTime.ToString("MMM dd, yyyy hh:mm:ss tt", new CultureInfo("en-US"));
        }

        public static string FormatWithUtcOffset(DateTime dateTime, TimeSpan? offset = null)
        {
            var value = dateTime;

            if (offset.HasValue)
            {
                value = dateTime.Add(offset.Value);
            }

            return Format(value);
        }

        public static TimeZoneInfo GetTimeZoneInfo(TimeSpan utcOffset)
        {
            var zones = TimeZoneInfo.GetSystemTimeZones();
            var zone = zones.FirstOrDefault(x => x.BaseUtcOffset == utcOffset);

            if (zone == null)
                throw new ArgumentOutOfRangeException(nameof(utcOffset), "There is no date with this UTC offset.");

            return zone;
        }

        public static string GetDaySuffix(int day)
        {
            switch (day)
            {
                case 1:
                case 21:
                case 31:
                    return "st";
                case 2:
                case 22:
                    return "nd";
                case 3:
                case 23:
                    return "rd";
                default:
                    return "th";
            }
        }
    }
}
