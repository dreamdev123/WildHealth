using System;

namespace WildHealth.Application.Utils.DateTimes
{
    /// <summary>
    /// Provides date time, used for date time usnit test support
    /// </summary>
    public interface IDateTimeProvider
    {
        DateTime Now();

        DateTime UtcNow();
    }
}
