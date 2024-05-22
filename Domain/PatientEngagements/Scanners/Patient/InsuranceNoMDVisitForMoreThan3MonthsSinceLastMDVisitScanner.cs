using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using static WildHealth.Domain.Enums.Appointments.AppointmentWithType;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.Patient;

/// <summary>
/// Insurance: No MD visit scheduled > 3 months since last MD visit.
/// </summary>
public class InsuranceNoMDVisitForMoreThan3MonthsSinceLastMDVisitScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public InsuranceNoMDVisitForMoreThan3MonthsSinceLastMDVisitScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .InsurancePatients()
            .MonthsSinceLastVisit(3, Provider, HealthCoachAndProvider)
            .Build();
    }
}