using System;
using TimeZoneConverter;

namespace WildHealth.Application.Utils.Timezones;

/// <summary>
/// The Helper includes methods for timezones convert
/// </summary>
public static class TimezoneHelper
{
    /// <summary>
    /// Convert Olson format timezone to C# system TimeZoneInfo
    /// </summary>
    /// <param name="timezone">Olson Timezone format</param>
    /// <returns>Nullable TimeZoneInfo</returns>
    public static TimeZoneInfo ToTimezoneInfo(string timezone)
    {
        return TZConvert.GetTimeZoneInfo(timezone);
    }

    /// <summary>
    /// Returns Windows timezone Id. If is not found returns UTC
    /// </summary>
    /// <param name="timeZoneId"></param>
    /// <returns></returns>
    public static string? GetWindowsId(string timeZoneId)
    {
        var isWindowsId = TZConvert.KnownWindowsTimeZoneIds.Contains(timeZoneId);
        if (isWindowsId)
        {
            return ApplyWindowsTimezoneGrouping(timeZoneId);
        }
        
        var innaConvert = TZConvert.TryIanaToWindows(timeZoneId, out var innaResult);
        if (innaConvert)
        {
            return ApplyWindowsTimezoneGrouping(innaResult);
        }

        var rubyConvert = TZConvert.TryRailsToWindows(timeZoneId, out var rubyResult);
        if (rubyConvert)
        {
            return ApplyWindowsTimezoneGrouping(rubyResult);
        }
        
        return null;
    }

    /// <summary>
    /// Returns Inna timezone id from TimeZoneInfo 
    /// </summary>
    /// <param name="timeZoneId"></param>
    /// <returns></returns>
    public static string? GetInnaId(string timeZoneId)
    {
        var isInnaId = TZConvert.KnownIanaTimeZoneNames.Contains(timeZoneId);
        if (isInnaId)
        {
            return timeZoneId;
        }
        
        var windowsConvert = TZConvert.TryWindowsToIana(timeZoneId, out var widowsResult);
        if (windowsConvert)
        {
            return widowsResult;
        }

        var rubyConvert = TZConvert.TryRailsToIana(timeZoneId, out var rubyResult);
        if (rubyConvert)
        {
            return rubyResult;
        }
        
        return null;
    }

    public static DateTime GetCurrentLocalTime(string timeZoneId)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
    }

    public static DateTime ConvertToTimeZone(string timeZoneId, DateTime inUtc)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        return TimeZoneInfo.ConvertTimeFromUtc(inUtc, timeZone);
    }

    private static string ApplyWindowsTimezoneGrouping(string timeZoneId)
    {
        switch (timeZoneId)
        {
            case "SA Pacific Standard Time":
            case "US Eastern Standard Time":
                return "Eastern Standard Time";
            default:
                return timeZoneId;
        }
    }
}