using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.HealthCoach;

public class LabsAreDueToBeDrawnScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public LabsAreDueToBeDrawnScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .TimeForLabs(EngagementDate.FromToDays(-3, 0))
            .Build();
    }
}