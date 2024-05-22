using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.HealthCoach;

public class NoImcForMoreThan1MonthAfterCheckoutScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public NoImcForMoreThan1MonthAfterCheckoutScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .DaysAfterCheckout(EngagementDate.FromToMonths(-2, -1))
            .NoVisits(
                days: EngagementDate.Any(), 
                withType: AppointmentWithType.Provider, 
                purpose: AppointmentPurpose.Consult)
            .NoVisits(
                days: EngagementDate.Any(), 
                withType: AppointmentWithType.HealthCoachAndProvider, 
                purpose: AppointmentPurpose.Consult)
            .Build();
    }
}