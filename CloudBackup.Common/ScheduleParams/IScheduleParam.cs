using System;

namespace CloudBackup.Common.ScheduleParams
{
    public interface IScheduleParam
    {
        RecurringPeriodType RecurringPeriodType { get; }

        /// <summary>
        /// Get scheduled date for now.
        /// </summary>
        /// <param name="datetime">Current next run date. VALUE NOT USED, only matters if it exists.</param>
        /// <returns>Scheduled date relative for now.</returns>
        DateTime? GetScheduledDateTime(DateTime? datetime);

        /// <summary>
        /// Get next run date after the previous one.
        /// </summary>
        /// <param name="prevRun">Last run date.</param>
        /// <returns>Next run date after the previous one.</returns>
        DateTime GetNextRun(DateTime prevRun);

        string FormatDatesToUserTimeZone(TimeSpan utcOffset);

        string ConvertDatesToUtc(TimeSpan utcOffset);

        bool IsPermittedToRunNow();

        string GetDescription();
    }

    public enum RecurringPeriodType
    {
        None,
        Unknown,
        Day,
        Week,
        Month,
        Year
    }
}