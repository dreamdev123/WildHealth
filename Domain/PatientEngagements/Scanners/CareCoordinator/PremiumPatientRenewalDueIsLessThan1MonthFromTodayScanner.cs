using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.CareCoordinator;

/// <summary>
// Patient is due a renewal < 1 month from today
/// </summary>
public class PremiumPatientRenewalDueIsLessThan1MonthFromTodayScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public PremiumPatientRenewalDueIsLessThan1MonthFromTodayScanner(EngagementCriteria criteria) : base(criteria) { }
    
    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());
        
        return query
            .RenewalIsInLessThan(months: 1)
            .Build();
    }
}