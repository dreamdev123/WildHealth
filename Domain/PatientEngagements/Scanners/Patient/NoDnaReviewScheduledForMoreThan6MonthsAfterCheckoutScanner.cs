using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.Patient;

/// <summary>
/// No DNA review scheduled > 6 months after checkout.
/// Patient has not had any provider visits.
/// </summary>
public class NoDnaReviewScheduledForMoreThan6MonthsAfterCheckoutScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public NoDnaReviewScheduledForMoreThan6MonthsAfterCheckoutScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .NoVisit(AppointmentTypes.PhysicianVisitDnaReview)
            .MonthsAfterCheckout(6)
            .Build();
    }
}