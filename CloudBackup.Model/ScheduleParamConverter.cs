using System;

namespace CloudBackup.Model
{
    public static class ScheduleParamConverter
    {
        public static string ConvertScheduleDatesToUserTimeZone(Schedule schedule, TimeSpan utcOffset)
        {
            var scheduleParam = ScheduleParamFactory.CreateScheduleParam(schedule);

            if (scheduleParam != null)
            {
                return scheduleParam.FormatDatesToUserTimeZone(utcOffset);
            }

            return string.Empty;
        }
    }
}
