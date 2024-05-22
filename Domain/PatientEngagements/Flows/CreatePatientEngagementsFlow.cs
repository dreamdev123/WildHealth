using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Domain.PatientEngagements.CommandHandlers;
using WildHealth.Application.Extensions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Models;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.PatientEngagements;
using WildHealth.IntegrationEvents.PatientEngagements.Payloads;
using static WildHealth.Domain.Entities.Engagement.EngagementAssignee;
using static WildHealth.Domain.Entities.Engagement.EngagementNotificationType;
using static WildHealth.Domain.Entities.Engagement.PatientEngagementStatus;

namespace WildHealth.Application.Domain.PatientEngagements.Flows;

public record CreatePatientEngagementsFlow(
    List<EngagementScannerAggregateResult> QualifiedForEngagement, 
    List<PatientEngagement> EngagementHistory, 
    Func<Guid, bool> IsNotificationsEnabledFunc,
    DateTime UtcNow, 
    Log Logger) : IMaterialisableFlow
{
    private ILookup<int, PatientEngagement>? _patientEngagementsInProgressLookup;
    private HashSet<(int, int)>? _notRepeatableHashSet;

    public MaterialisableFlowResult Execute()
    {
        var candidates = QualifiedForEngagement
            .Where(HasNoOtherPatientEngagementsInProgress)
            .Where(HasNotBeenExecutedRecently) // Patient cannot qualify for the same group more than once every 30 days;
            .ToArray(); 
        
        var careCoordinatorEngagements = candidates
            .Where(e => (e.Criteria.Assignee & CareCoordinator) == CareCoordinator)
            .ToArray();
        
        var healthCoachEngagements = candidates
            .Where(e => (e.Criteria.Assignee & HealthCoach) == HealthCoach)
            .ToArray();

        var patientEngagements = GetPatientEngagements(candidates);

        var newPatientEngagementsRaw = careCoordinatorEngagements
            .Concat(patientEngagements)
            .Concat(healthCoachEngagements)
            .Distinct()
            .ToArray();
        
        var newPatientEngagements = newPatientEngagementsRaw
            .Select(BuildPatientEngagement)
            .Select(pe => pe.Added())
            .ToFlowResult();

        return 
            newPatientEngagements + 
            ResurrectedItems + 
            new PatientsInNeedOfEngagementFoundEvent() +
            BuildIntegrationEvents(newPatientEngagementsRaw);
    }

    /// <summary>
    /// Regular Patient: If patient was in one group, they won’t qualify for an additional group another 2 weeks
    /// Premium Patient: Can have multiple engagement items at the same time
    /// </summary>
    private bool HasNoOtherPatientEngagementsInProgress(EngagementScannerAggregateResult newEngagement)
    {
        if (newEngagement.Criteria.Assignee == Patient)
        {
            return PatientEngagementsInProgress[newEngagement.PatientId]
                .NotExists(x => x.EngagementCriteria.Assignee == newEngagement.Criteria.Assignee);
        }
        
        return PatientEngagementsInProgress[newEngagement.PatientId] // there can be more then one engagement item active for a Premium patient 
            .NotExists(pe => pe.EngagementCriteriaId == newEngagement.Criteria.Id);
    }

    /// <summary>
    /// Ensure current patient in not in the "Not Repeatable" group
    /// </summary>
    private bool HasNotBeenExecutedRecently(EngagementScannerAggregateResult x) => 
        !RecentEngagementAttemptsThatCannotBeRepeatedNow.Contains((x.PatientId, x.Criteria.GetId()));

    /// <summary>
    /// Get all the ongoing patient engagements 
    /// </summary>
    private ILookup<int, PatientEngagement> PatientEngagementsInProgress => 
        _patientEngagementsInProgressLookup ??= EngagementHistory
            .Where(x => x.NotExpired(UtcNow))
            .ToLookup(pe => pe.PatientId, pe => pe);

    /// <summary>
    /// Gets Patient audience items filtered by priority and checks if Notifications enabled for a particular user. 
    /// </summary>
    private IEnumerable<EngagementScannerAggregateResult> GetPatientEngagements(EngagementScannerAggregateResult[] source)
    {
        var allPatientCriteria = source
            .Where(e => (e.Criteria.Assignee & Patient) == Patient)
            .DistinctBy( // If patient qualifies for multiple groups, pick highest priority 
                property: x => x.PatientId,
                orderBy: x => x.Criteria.Priority)
            .ToArray();

        // we don't check if Notifications enabled for 'Dashboard' notifications type because this type doesn't imply EMAIL/SMS notifications
        var dashboard = allPatientCriteria
            .Where(e => e.Criteria.NotificationType == Dashboard);

        var emailSms = allPatientCriteria
            .Where(e => 
                (e.Criteria.NotificationType & SMS) == SMS || 
                (e.Criteria.NotificationType & Email) == Email)
            .ToArray();

        var enabledEmailSms = emailSms
            .Where(e => IsNotificationsEnabledFunc(e.PatientUniversalId)) // check if notifications enabled for this user
            .ToArray();
        
        Logger.Invoke($"Found none premium engagements total count: {emailSms.Length}; Enabled count: {enabledEmailSms.Length}");
        
        return dashboard.Concat(enabledEmailSms);
    }
    
    /// <summary>
    /// Gets all (PatientI, EngagementCriteriaId) pairs that can't be repeated because of 
    /// Lock Period (usually 30 days) or can run at most once and already used their shot
    /// </summary>
    private HashSet<(int, int)> RecentEngagementAttemptsThatCannotBeRepeatedNow =>
        _notRepeatableHashSet ??= EngagementHistory
            .Where(x =>
                 x.Expired(UtcNow) &&
                 (x.EngagementCriteria.RepeatInDays == 0 || // can run at most once and already used their shot
                 (UtcNow.Date - x.CreatedAt.Date).Days <= x.EngagementCriteria.RepeatInDays)) // Patient cannot qualify for the same group more than once every 30 days
            .Select(x => (x.PatientId, x.EngagementCriteriaId))
            .ToHashSet();

    /// <summary>
    /// Patient Tasks that have been completed (Appointment booked) but eventually cancelled.
    /// We reset status back to 'InProgress' for these.
    /// </summary>
    private IEnumerable<EntityAction> ResurrectedItems => SucceededButNotExpired
        .Where(pe =>
            QualifiedForEngagement.Any(q =>
                pe.PatientId == q.PatientId &&
                pe.EngagementCriteriaId == q.Criteria.GetId()))
        .Select(Resurrect)
        .ToArray();

    private IEnumerable<PatientEngagement> SucceededButNotExpired => 
        EngagementHistory.Where(x => 
            x.NotExpired(UtcNow) && 
            x.Completed());

    private EntityAction Resurrect(PatientEngagement engagement)
    {
        engagement.Status = InProgress;
        return engagement.Updated();
    }

    private PatientEngagement BuildPatientEngagement(EngagementScannerAggregateResult src) => new()
    {
        PatientId = src.PatientId,
        EngagementCriteriaId = src.Criteria.GetId(),
        EngagementCriteria = src.Criteria,
        Status = src.Criteria.NotificationType == Dashboard ? InProgress : PendingAction, 
        IsPremium = src.IsPremium,
        ExpirationDate = src.Criteria.EngagementPeriodInDays == int.MaxValue ? DateTime.MaxValue : UtcNow.Date + TimeSpan.FromDays(src.Criteria.EngagementPeriodInDays)
    };
    
    private IEnumerable<BaseIntegrationEvent> BuildIntegrationEvents(EngagementScannerAggregateResult[] newPatientEngagements)
    {
        return 
            from patientEngagement in newPatientEngagements 
            where !string.IsNullOrEmpty(patientEngagement.Criteria.AnalyticsEvent) 
            select new PatientQualifiedForEngagementPayload(patientEngagement.Criteria.AnalyticsEvent, patientEngagement.PatientUniversalId.ToString()) into payload 
            select new PatientEngagementIntegrationEvent(payload, UtcNow);
    }
}