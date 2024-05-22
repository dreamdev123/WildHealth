using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.CareCoordinator;

/// <summary>
/// No ICC scheduled > 2 weeks after checkout.
/// </summary>
public class PremiumNoICCForMoreThan2WeeksAfterCheckoutScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public PremiumNoICCForMoreThan2WeeksAfterCheckoutScanner(EngagementCriteria criteria) : base(criteria) { }
    
    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .NoVisit(AppointmentWithType.HealthCoach)
            .DaysAfterCheckout(14) // 2 weeks
            .Build();
    }
}