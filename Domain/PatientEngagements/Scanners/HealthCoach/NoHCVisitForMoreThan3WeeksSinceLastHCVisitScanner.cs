using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.HealthCoach;

public class NoHcVisitForMoreThan3WeeksSinceLastHcVisitScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public NoHcVisitForMoreThan3WeeksSinceLastHcVisitScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());
        
        return query
            .VisitCompleted(
                days: EngagementDate.FromToDays(-28, -21),
                types: new []
                {
                    AppointmentWithType.HealthCoach
                })
            .NoVisits(
                days: EngagementDate.SinceDays(-21), 
                withType: AppointmentWithType.HealthCoach, 
                purpose: AppointmentPurpose.FollowUp)
            .Build();
    }
}