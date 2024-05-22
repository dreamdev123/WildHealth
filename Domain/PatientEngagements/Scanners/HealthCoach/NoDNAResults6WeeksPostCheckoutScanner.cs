using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.HealthCoach;

public class NoDnaResults6WeeksPostCheckoutScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public NoDnaResults6WeeksPostCheckoutScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());
        
        return query
            .NoDnaResults()
            .DaysAfterCheckout(EngagementDate.FromToDays(-56, -42))
            .Build();
    }
}