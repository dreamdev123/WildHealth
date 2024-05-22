using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using static WildHealth.Domain.Enums.Appointments.AppointmentWithType;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.Patient;

/// <summary>
/// Cash: No MD visit scheduled > 6 months since last MD visit.
/// No messages in last 2 weeks
/// </summary>
public class CashNoMDVisitForMoreThan6MonthsSinceLastMDVisitScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public CashNoMDVisitForMoreThan6MonthsSinceLastMDVisitScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());
        
        return query
            .CashPatients()
            .MonthsSinceLastVisit(3, Provider, HealthCoachAndProvider)
            .Build();
    }
}