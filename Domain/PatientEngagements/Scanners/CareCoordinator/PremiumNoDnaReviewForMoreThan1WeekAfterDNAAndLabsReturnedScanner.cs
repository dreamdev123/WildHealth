using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.CareCoordinator;

/// <summary>
/// No Dna Review scheduled > 1 week after DNA & Labs returned
/// </summary>
public class PremiumNoDnaReviewForMoreThan1WeekAfterDNAAndLabsReturnedScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public PremiumNoDnaReviewForMoreThan1WeekAfterDNAAndLabsReturnedScanner(EngagementCriteria criteria) : base(criteria)
    {
    }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .DaysAfterDnaAndLabsReturned(7)
            .NoVisit(AppointmentTypes.PhysicianVisitDnaReview)
            .Build();
    }
}