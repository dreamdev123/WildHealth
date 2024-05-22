using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.CareCoordinator;

/// <summary>
/// Patient Birthday is Today
/// </summary>
public class PremiumPatientBirthdayIsTodayScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public PremiumPatientBirthdayIsTodayScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());
        
        return query
            .BirthdayToday()
            .Build();
    }
}