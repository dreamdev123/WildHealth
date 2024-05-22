using System;
using WildHealth.Domain.Entities.Engagement;

namespace WildHealth.Application.Domain.PatientEngagements;

public static class PatientEngagementDomainExtensions
{
    private static bool IsExpired(this PatientEngagement source, DateTime timestamp) =>
        source.ExpirationDate.Date <= timestamp.Date;
    
    public static bool Expired(this PatientEngagement source, DateTime timestamp) =>
        source.IsExpired(timestamp);

    public static bool NotExpired(this PatientEngagement source, DateTime timestamp) =>
        !source.IsExpired(timestamp);
    
    public static bool Completed(this PatientEngagement source) =>
        source.Status == PatientEngagementStatus.Completed;
}