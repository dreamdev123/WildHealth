using System;
using System.Linq;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using static WildHealth.Domain.Enums.Appointments.AppointmentWithType;

namespace WildHealth.Application.Domain.PatientEngagements.Scanners.CareCoordinator;

/// <summary>
/// No IMC scheduled > 1 week after DNA & Labs returned
/// </summary>
public class PremiumNoIMCForMoreThan1WeekAfterDNAAndLabsReturnedScanner : EngagementCriteriaScanner, IEngagementCriteriaScanner
{
    public PremiumNoIMCForMoreThan1WeekAfterDNAAndLabsReturnedScanner(EngagementCriteria criteria) : base(criteria) { }
    
    public IQueryable<EngagementScannerResult> Scan(IQueryable<Subscription> source, DateTime timestamp)
    {
        var query = new EngagementCriteriaQueryBuilder(source, timestamp, Criteria());
  
        return query
            .NoVisitDaysAfterDnaAndLabsReturned(7, Provider, HealthCoachAndProvider)
            .Build();
    }
}