using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.Patient;

/// <summary>
/// IMC Completed 24 hours ago.
/// Patient has no upcoming HC appointments scheduled.
/// </summary>
public class IMCCompleted1DayAgoScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public IMCCompleted1DayAgoScanner(EngagementCriteria criteria) : base(criteria) { }

    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());

        return query
            .IMCCompleted(daysAgo: 1)
            .NoNewVisit(sinceDaysAgo: 1, AppointmentWithType.HealthCoach)
            .Build();
    }
}