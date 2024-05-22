using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.Patient;

/// <summary>
/// No Dna review scheduled > 1 day after DNA & Labs returned.
/// Patient has not had any provider visits
/// </summary>
public class NoDnaReviewForMoreThan1DayAfterDNAAndLabsReturnedScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public NoDnaReviewForMoreThan1DayAfterDNAAndLabsReturnedScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());
  
        return query
            .DaysAfterDnaAndLabsReturned(1)
            .NoVisit(AppointmentTypes.PhysicianVisitDnaReview)
            .Build();
    }
}