using System;

namespace WildHealth.Application.Utils.DateTimes
{
    public class DateTimeProvider: IDateTimeProvider
    {
        public DateTime Now() => DateTime.Now;

        public DateTime UtcNow() => DateTime.UtcNow;
    }
}
