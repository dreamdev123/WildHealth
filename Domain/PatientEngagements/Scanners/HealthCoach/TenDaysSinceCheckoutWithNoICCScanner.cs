using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.HealthCoach;

public class TenDaysSinceCheckoutWithNoIccScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public TenDaysSinceCheckoutWithNoIccScanner(EngagementCriteria criteria) : base(criteria) { }
    
    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());
        
        return query
            .DaysAfterCheckout(EngagementDate.FromToDays(-14, -10))
            .NoVisits(
                days: EngagementDate.Any(), 
                withType: AppointmentWithType.HealthCoach, 
                purpose: AppointmentPurpose.Consult)
            .Build();
    }
}