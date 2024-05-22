using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.Patient;

/// <summary>
/// DNA review Completed 24 hours ago.
/// Patient has no upcoming HC appointments scheduled.
/// </summary>
public class DnaReviewCompleted1DayAgoScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public DnaReviewCompleted1DayAgoScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .VisitCompleted(daysAgo: 1, AppointmentTypes.PhysicianVisitDnaReview)
            .NoNewVisit(sinceDaysAgo: 1, AppointmentWithType.HealthCoach)
            .Build();
    }
}