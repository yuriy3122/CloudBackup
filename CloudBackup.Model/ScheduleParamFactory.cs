using Newtonsoft.Json;
using CloudBackup.Common.ScheduleParams;

namespace CloudBackup.Model
{
    public class ScheduleParamFactory
    {
        public static IScheduleParam? CreateScheduleParam(Schedule schedule)
        {
            Func<IScheduleParam?>? deserializer = null;

            if (schedule.StartupType == StartupType.Delayed)
            {
                deserializer = () => JsonConvert.DeserializeObject<DelayedScheduleParam>(schedule.Params);
            }
            else if (schedule.StartupType == StartupType.Recurring)
            {
                if (schedule.OccurType == OccurType.Periodically)
                {
                    deserializer = () => JsonConvert.DeserializeObject<IntervalScheduleParam>(schedule.Params);
                }
                else if (schedule.OccurType == OccurType.Daily)
                {
                    deserializer = () => JsonConvert.DeserializeObject<DailyScheduleParam>(schedule.Params);
                }
                else if (schedule.OccurType == OccurType.Monthly)
                {
                    deserializer = () => JsonConvert.DeserializeObject<MonthlyScheduleParam>(schedule.Params);
                }
            }

            if (deserializer != null && string.IsNullOrEmpty(schedule.Params))
                throw new ArgumentException($"{nameof(schedule)}.{nameof(schedule.Params)}");

            return deserializer?.Invoke();
        }
    }
}