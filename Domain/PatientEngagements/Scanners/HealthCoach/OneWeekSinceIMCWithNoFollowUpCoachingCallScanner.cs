using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.HealthCoach;

public class OneWeekSinceImcWithNoFollowUpCoachingCallScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public OneWeekSinceImcWithNoFollowUpCoachingCallScanner(EngagementCriteria criteria) : base(criteria) { }
    
    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());
        
        return query
            .VisitCompleted(
                days: EngagementDate.FromToDays(-14, -7),
                types: new []
                {
                    AppointmentWithType.Provider,
                    AppointmentWithType.HealthCoachAndProvider
                })
            .NoVisits(
                days: EngagementDate.Any(), 
                withType: AppointmentWithType.HealthCoach, 
                purpose: AppointmentPurpose.FollowUp)
            .Build();
    }
}