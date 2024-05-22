using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using static WildHealth.Domain.Enums.Appointments.AppointmentWithType;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.Patient;

/// <summary>
/// No IMC Scheduled > 2 months after checkout.
/// </summary>
public class NoIMCForMoreThan2MonthsAfterCheckoutScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public NoIMCForMoreThan2MonthsAfterCheckoutScanner(EngagementCriteria criteria) : base(criteria) {  }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .NoVisit(Provider, HealthCoachAndProvider)
            .MonthsAfterCheckout(2)
            .Build();
    }
}