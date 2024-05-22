using System;

namespace WildHealth.Application.Domain.PatientEngagements;

public class EngagementDate
{
    public int? From { get; }
    
    public int? To { get; }
    
    public string Units { get; }

    public (DateTime? minDate, DateTime? maxDate) ToDate(DateTime utcNow)
    {
        DateTime? minDate = From.HasValue ? Units switch
        {
            "days" => utcNow.AddDays(From.Value).Date,
            "months" => utcNow.AddMonths(From.Value).Date,
            _ => throw new ArgumentOutOfRangeException()
        } : null;
        
        DateTime? maxDate = To.HasValue ? Units switch
        {
            "days" => utcNow.AddDays(To.Value).Date,
            "months" => utcNow.AddMonths(To.Value).Date,
            _ => throw new ArgumentOutOfRangeException()
        } : null;

        return (minDate, maxDate);
    }

    private EngagementDate(int? from, int? to, string units)
    {
        From = from;
        To = to;
        Units = units;
    }

    public static EngagementDate SinceDays(int since)
    {
        return new EngagementDate(since, null, "days");
    }
    
    public static EngagementDate SinceMonths(int since)
    {
        return new EngagementDate(since, null, "months");
    }
    
    public static EngagementDate FromToDays(int from, int to)
    {
        return new EngagementDate(from, to, "days");
    }
    
    public static EngagementDate FromToMonths(int from, int to)
    {
        return new EngagementDate(from, to, "months");
    }
    
    public static EngagementDate Any()
    {
        return new EngagementDate(null, null, string.Empty);
    }
}