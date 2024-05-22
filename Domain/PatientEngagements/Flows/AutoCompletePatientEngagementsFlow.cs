using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Enums.Appointments;
using static WildHealth.Domain.Entities.Engagement.EngagementCriteriaType;

namespace WildHealth.Application.Domain.PatientEngagements.Flows;

public record AutoCompletePatientEngagementsFlow(AppointmentWithType AppointmentType, 
    List<PatientEngagement> PatientTasks, 
    DateTime UtcNow) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var activeTasks = PatientTasks.Where(x => 
            x.NotExpired(UtcNow) &&
            x.Status is PatientEngagementStatus.PendingAction or PatientEngagementStatus.InProgress);
    
        return activeTasks.Select(Complete)
            .ToArray()
            .ToFlowResult();
    }

    private EntityAction Complete(PatientEngagement task)
    {
        return AppointmentType switch
        {
            AppointmentWithType.Provider or AppointmentWithType.HealthCoachAndProvider when ProviderCriteria.Contains(task.EngagementCriteria.Type) => ChangeStatus(task).Updated(),
            AppointmentWithType.HealthCoach when HealthCoachCriteria.Contains(task.EngagementCriteria.Type) => ChangeStatus(task).Updated(),
            _ => EntityAction.None.Instance
        };
    }

    private static PatientEngagement ChangeStatus(PatientEngagement task)
    {
        task.Status = PatientEngagementStatus.Completed;
        task.CompletedBy = 0; // 0 means System
        return task;
    }

    private static readonly EngagementCriteriaType[] ProviderCriteria = 
    {
        NoIMCForMoreThan2MonthsAfterCheckout, 
        NoIMCForMoreThan6MonthsAfterCheckout,
        InsuranceNoMDVisitForMoreThan3MonthsSinceLastMDVisit,
        CashNoMDVisitForMoreThan3MonthsSinceLastMDVisit,
        NoIMCForMoreThan1DayAfterDNAAndLabsReturned,
        NoIMCForMoreThan2WeeksAfterDNAAndLabsReturned,
        PremiumNoMDVisitForMoreThan1MonthSinceLastMDVisit,
        PremiumNoIMCForMoreThan1WeekAfterDNAAndLabsReturned,
        PremiumNoIMCForMoreThan2MonthsAfterCheckout,
        NoIMCForMoreThan1MonthAfterCheckout,
        NoMDVisitForMoreThan80DaysSinceLastMDVisit,
        
        // Both Coach and Provider
        NoICCForMoreThan1DayAfterHCAssigned,
        NoICCForMoreThan2WeeksAfterCheckout,
        NoVisitsOrMessagesSentIn14Days
    };
    
    private static readonly EngagementCriteriaType[] HealthCoachCriteria = 
    {
        NoHCVisitForMoreThanMonthSinceLastHC,
        NoICCForMoreThan2WeeksAfterCheckout,
        NoICCForMoreThan1DayAfterHCAssigned,
        IMCCompleted1DayAgo,
        PremiumNoHCVisitForMoreThanMonthSinceLastHC,
        PremiumNoICCForMoreThan2WeeksAfterCheckout,
        TenDaysSinceCheckoutWithNoICC,
        OneWeekSinceIMCWithNoFollowupCoachingCall,
        NoVisitsOrMessagesSentIn14Days,
        NoHCVisitForMoreThan3WeeksSinceLastHCVisit
    };
}