using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.Patient;

/// <summary>
/// No HC visit scheduled > 1 month since last HC visit
/// IMC has been completed
/// </summary>
public class NoHCVisitForMoreThan1MonthSinceLastHCVisitScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public NoHCVisitForMoreThan1MonthSinceLastHCVisitScanner(EngagementCriteria criteria): base(criteria) { }
    
    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .MonthsSinceLastVisit(1, AppointmentWithType.HealthCoach)
            .IMCCompleted()
            .Build();
    }
}