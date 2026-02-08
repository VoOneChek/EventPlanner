using System;
using System.Collections.Generic;
using System.Linq;

namespace EventPlannerWebApplication.Services
{
    public interface ITimeService
    {
        DateTime ConvertToUtc(DateTime localTime, int timezoneOffsetMinutes);

        List<DateTime> ConvertToUtc(IEnumerable<DateTime> localTimes, int timezoneOffsetMinutes);
    }

    public class TimeService : ITimeService
    {
        /// <summary>
        /// Переводит локальное время в UTC.
        /// </summary>
        /// <param name="localTime">Время из input type="datetime-local"</param>
        /// <param name="timezoneOffsetMinutes">Смещение из JS (getTimezoneOffset). Например, -180 для Москвы (UTC+3)</param>
        public DateTime ConvertToUtc(DateTime localTime, int timezoneOffsetMinutes)
        {
            var utcTime = localTime.AddMinutes(timezoneOffsetMinutes);

            return DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        }

        public List<DateTime> ConvertToUtc(IEnumerable<DateTime> localTimes, int timezoneOffsetMinutes)
        {
            return localTimes
                .Select(t => ConvertToUtc(t, timezoneOffsetMinutes))
                .ToList();
        }
    }
}
