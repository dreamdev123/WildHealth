using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Extensions;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Domain.PatientEngagements;

public interface IEngagementCriteriaScanner
{
    IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp);
    EngagementCriteria Criteria();
}

public abstract class EngagementCriteriaScanner 
{
    private readonly EngagementCriteria _criteria;

    protected EngagementCriteriaScanner(EngagementCriteria criteria)
    {
        _criteria = criteria;
    }

    public EngagementCriteria Criteria() => _criteria;
}

public record EngagementScannerResult(int PatientId, Guid PatientUniversalId, bool IsPremium)
{
    public static List<EngagementScannerResult> Empty => List.Empty<EngagementScannerResult>();
};

