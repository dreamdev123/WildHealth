using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using static WildHealth.Domain.Enums.Appointments.AppointmentWithType;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.CareCoordinator;

/// <summary>
/// No MD visit scheduled > 1 months since last MD visit
/// </summary>
public class PremiumNoMDVisitForMoreThan1MonthSinceLastMDVisitScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public PremiumNoMDVisitForMoreThan1MonthSinceLastMDVisitScanner(EngagementCriteria criteria) : base(criteria) { }
    
    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .MonthsSinceLastVisit(1, Provider, HealthCoachAndProvider)
            .Build();
    }
}