using System;
using WildHealth.Application.Domain.PatientEngagements.Scanners.CareCoordinator;
using WildHealth.Application.Domain.PatientEngagements.Scanners.HealthCoach;
using WildHealth.Application.Domain.PatientEngagements.Scanners.Patient;
using WildHealth.Domain.Entities.Engagement;
using static WildHealth.Domain.Entities.Engagement.EngagementCriteriaType;

namespace WildHealth.Application.Domain.PatientEngagements;

public static class EngagementCriteriaScannerFactory
{
    public static IEngagementCriteriaScanner CreateScanner(EngagementCriteria criteria)
    {
        return criteria.Type switch
        {
            NoIMCForMoreThan2MonthsAfterCheckout =>
                new NoIMCForMoreThan2MonthsAfterCheckoutScanner(criteria),
            NoDnaReviewForMoreThan2MonthsAfterCheckout =>
                new NoDnaReviewForMoreThan2MonthsAfterCheckoutScanner(criteria),
            InsuranceNoMDVisitForMoreThan3MonthsSinceLastMDVisit => 
                new InsuranceNoMDVisitForMoreThan3MonthsSinceLastMDVisitScanner(criteria),
            CashNoMDVisitForMoreThan3MonthsSinceLastMDVisit =>
                new CashNoMDVisitForMoreThan6MonthsSinceLastMDVisitScanner(criteria),
            NoHCVisitForMoreThanMonthSinceLastHC =>
                new NoHCVisitForMoreThan1MonthSinceLastHCVisitScanner(criteria),
            NoICCForMoreThan2WeeksAfterCheckout => 
                new NoICCForMoreThan2WeeksAfterCheckoutScanner(criteria),
            NoIMCForMoreThan6MonthsAfterCheckout => 
                new NoIMCScheduledForMoreThan6MonthsAfterCheckoutScanner(criteria),
            NoDnaReviewForMoreThan6MonthsAfterCheckout => 
                new NoDnaReviewScheduledForMoreThan6MonthsAfterCheckoutScanner(criteria),
            NoIMCForMoreThan1DayAfterDNAAndLabsReturned =>
                new NoIMCForMoreThan1DayAfterDNAAndLabsReturnedScanner(criteria),
            NoDnaReviewForMoreThan1DayAfterDNAAndLabsReturned =>
                new NoDnaReviewForMoreThan1DayAfterDNAAndLabsReturnedScanner(criteria),
            NoIMCForMoreThan2WeeksAfterDNAAndLabsReturned => 
                new NoIMCForMoreThan2WeeksAfterDNAAndLabsReturnedScanner(criteria),
            NoDnaReviewForMoreThan2WeeksAfterDNAAndLabsReturned => 
                new NoDnaReviewForMoreThan2WeeksAfterDNAAndLabsReturnedScanner(criteria),
            NoICCForMoreThan1DayAfterHCAssigned =>
                new NoICCForMoreThan1DayAfterHCAssignedScanner(criteria),
            IMCCompleted1DayAgo => 
                new IMCCompleted1DayAgoScanner(criteria),
            DnaReviewCompleted1DayAgo => 
                new DnaReviewCompleted1DayAgoScanner(criteria),
            PremiumPatientBirthdayIsToday => 
                new PremiumPatientBirthdayIsTodayScanner(criteria),
            PremiumNoMDVisitForMoreThan1MonthSinceLastMDVisit => 
                new PremiumNoMDVisitForMoreThan1MonthSinceLastMDVisitScanner(criteria),
            PremiumNoHCVisitForMoreThanMonthSinceLastHC => 
                new PremiumNoHCVisitForMoreThanMonthSinceLastHCScanner(criteria),
            PremiumMoreThan14DaysSinceLastClarityMessageReceived => 
                new PremiumMoreThan14DaysSinceLastClarityMessageReceivedScanner(criteria),
            PremiumNoICCForMoreThan2WeeksAfterCheckout => 
                new PremiumNoICCForMoreThan2WeeksAfterCheckoutScanner(criteria),
            PremiumNoIMCForMoreThan1WeekAfterDNAAndLabsReturned => 
                new PremiumNoIMCForMoreThan1WeekAfterDNAAndLabsReturnedScanner(criteria),
            PremiumNoDnaReviewForMoreThan1WeekAfterDNAAndLabsReturned => 
                new PremiumNoDnaReviewForMoreThan1WeekAfterDNAAndLabsReturnedScanner(criteria),
            PremiumNoIMCForMoreThan2MonthsAfterCheckout => 
                new PremiumNoIMCForMoreThan2MonthsAfterCheckoutScanner(criteria),
            PremiumNoDnaReviewForMoreThan2MonthsAfterCheckout => 
                new PremiumNoDnaReviewForMoreThan2MonthsAfterCheckoutScanner(criteria),
            PremiumPatientRenewalDueIsLessThan1MonthFromToday => 
                new PremiumPatientRenewalDueIsLessThan1MonthFromTodayScanner(criteria),
            
            // HealthCoach
            TenDaysSinceCheckoutWithNoICC => 
                new TenDaysSinceCheckoutWithNoIccScanner(criteria),
            OneWeekSinceIMCWithNoFollowupCoachingCall =>
                new OneWeekSinceImcWithNoFollowUpCoachingCallScanner(criteria),
            NoVisitsOrMessagesSentIn14Days =>
                new NoVisitsOrMessagesSentIn14DaysScanner(criteria),
            NoIMCForMoreThan1MonthAfterCheckout => 
                new NoImcForMoreThan1MonthAfterCheckoutScanner(criteria),
            NoDnaReviewForMoreThan1MonthAfterCheckout => 
                new NoDnaReviewForMoreThan1MonthAfterCheckoutScanner(criteria),
            NoDNAResults6weeksPostCheckout =>
                new NoDnaResults6WeeksPostCheckoutScanner(criteria),
            NoMDVisitForMoreThan80DaysSinceLastMDVisit => 
                new NoMdVisitForMoreThen80DaysSinceLastMdVisitScanner(criteria),
            LabsAreDueToBeDrawn =>
                new LabsAreDueToBeDrawnScanner(criteria),
            NoHCVisitForMoreThan3WeeksSinceLastHCVisit => 
                new NoHcVisitForMoreThan3WeeksSinceLastHcVisitScanner(criteria),
            
            _ => throw new ArgumentException($"Invalid criteria type [{criteria.Type}].")
        };
    }
}