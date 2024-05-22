using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;
using static WildHealth.Domain.Enums.Appointments.AppointmentWithType;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.HealthCoach;

public class NoVisitsOrMessagesSentIn14DaysScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public NoVisitsOrMessagesSentIn14DaysScanner(EngagementCriteria criteria) : base(criteria) { }
    
    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .DaysAfterCheckout(14)
            .DaysSinceLastClarityMessage(14)
            .NoNewVisit(sinceDaysAgo: 14, AppointmentWithType.HealthCoach, Provider, HealthCoachAndProvider)
            .Build();
    }
}