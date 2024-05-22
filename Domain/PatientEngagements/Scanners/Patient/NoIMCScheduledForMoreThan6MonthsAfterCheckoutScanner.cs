using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using static WildHealth.Domain.Enums.Appointments.AppointmentWithType;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.Patient;

/// <summary>
/// No IMC scheduled > 6 months after checkout.
/// Patient has not had any provider visits.
/// </summary>
public class NoIMCScheduledForMoreThan6MonthsAfterCheckoutScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public NoIMCScheduledForMoreThan6MonthsAfterCheckoutScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .NoVisit(Provider, HealthCoachAndProvider)
            .MonthsAfterCheckout(6)
            .Build();
    }
}