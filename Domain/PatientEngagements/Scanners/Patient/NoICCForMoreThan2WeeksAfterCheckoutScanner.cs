using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;
using static WildHealth.Domain.Enums.Appointments.AppointmentWithType;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.Patient;

/// <summary>
/// No ICC scheduled > 2 weeks after checkout.
/// Patient has not had any visits (coaching or provider)
/// </summary>
public class NoICCForMoreThan2WeeksAfterCheckoutScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public NoICCForMoreThan2WeeksAfterCheckoutScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .NoVisit(AppointmentWithType.HealthCoach, Provider, HealthCoachAndProvider)
            .DaysAfterCheckout(14) // 2 weeks
            .Build();
    }
}