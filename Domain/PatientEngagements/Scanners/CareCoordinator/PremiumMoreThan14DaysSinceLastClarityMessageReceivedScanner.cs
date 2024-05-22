using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.CareCoordinator;

/// <summary>
/// > 14 Days since last Clarity message received
/// </summary>
public class PremiumMoreThan14DaysSinceLastClarityMessageReceivedScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public PremiumMoreThan14DaysSinceLastClarityMessageReceivedScanner(EngagementCriteria criteria) : base(criteria) { }
    
    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .DaysAfterCheckout(14)
            .DaysSinceLastClarityMessage(14)
            .Build();
    }
}