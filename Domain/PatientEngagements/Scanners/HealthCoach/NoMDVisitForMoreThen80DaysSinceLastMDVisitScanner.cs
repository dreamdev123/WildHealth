using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.HealthCoach;

public class NoMdVisitForMoreThen80DaysSinceLastMdVisitScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public NoMdVisitForMoreThen80DaysSinceLastMdVisitScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());
        
        return query
            .VisitCompleted(
                days: EngagementDate.FromToDays(-89, -81), 
                types: new []
                {
                    AppointmentWithType.Provider,
                    AppointmentWithType.HealthCoachAndProvider
                })
            .NoVisits(
                days: EngagementDate.Any(), 
                withType: AppointmentWithType.Provider, 
                purpose: AppointmentPurpose.FollowUp)
            .Build();
    }
}