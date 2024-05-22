using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.CareCoordinator;

/// <summary>
/// No Dna review Scheduled > 2 months after checkout
/// </summary>
public class PremiumNoDnaReviewForMoreThan2MonthsAfterCheckoutScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public PremiumNoDnaReviewForMoreThan2MonthsAfterCheckoutScanner(EngagementCriteria criteria) : base(criteria) { }
    
    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .NoVisit(AppointmentTypes.PhysicianVisitDnaReview)
            .MonthsAfterCheckout(2)
            .Build();
    }
}