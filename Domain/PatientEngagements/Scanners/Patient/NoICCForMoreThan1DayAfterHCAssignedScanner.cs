using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;
using static WildHealth.Domain.Enums.Appointments.AppointmentWithType;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.Patient;

/// <summary>
/// No ICC Scheduled > 1 day after HC is assigned.
/// Patient has not had any visits (coaching or provider)
/// </summary>
public class NoICCForMoreThan1DayAfterHCAssignedScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public NoICCForMoreThan1DayAfterHCAssignedScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .DaysAfterHCAssigned(1)
            .NoVisit(AppointmentWithType.HealthCoach, Provider, HealthCoachAndProvider)
            .Build();
    }
}