using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.HealthCoach;

public class NoDnaReviewForMoreThan1MonthAfterCheckoutScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public NoDnaReviewForMoreThan1MonthAfterCheckoutScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .DaysAfterCheckout(EngagementDate.FromToMonths(-2, -1))
            .NoVisit(AppointmentTypes.PhysicianVisitDnaReview)
            .Build();
    }
}