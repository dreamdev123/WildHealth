using System;
using OneOf.Monads;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.PatientJourney;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Domain.Entities.PatientJourney;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Extensions;
using WildHealth.IntegrationEvents.PatientJourney;
using WildHealth.IntegrationEvents.PatientJourney.Payloads;

namespace WildHealth.Application.Domain.PatientJourney.Flows;

public record CompletePatientJourneyTaskFlow(int PatientId,
    JourneyTask JourneyTask,
    Option<PatientJourneyTask> PatientJourneyTask,
    JourneyTaskCompletedBy CompletedBy,
    User User,
    PatientJourneyTree PatientJourneyTree,
    string DashboardUrl,
    DateTime Now, 
    bool FeatureFlagEnabled) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (!FeatureFlagEnabled)
            return MaterialisableFlowResult.Empty;
        
        var completedTask = PatientJourneyTask.HasValue() ? CompleteJourneyTask() : AddAndCompleteJourneyTask();
    
        if (completedTask == MaterialisableFlowResult.Empty) 
            return completedTask;
        
        return completedTask + RewardUnlockedNotification() + BuildIntegrationEvent();
    }

    private MaterialisableFlowResult AddAndCompleteJourneyTask()
    {
        return new PatientJourneyTask
        {
            JourneyTaskId = JourneyTask.GetId(),
            PatientId = PatientId,
            Status = GetNewStatus()
        }.Added();
    }

    private MaterialisableFlowResult CompleteJourneyTask()
    {
        var task = PatientJourneyTask.Value();
        
        var newStatus = GetNewStatus();
        if (task.Status == PatientJourneyTaskStatus.Dismissed || task.Status == newStatus) // status unchanged
            return MaterialisableFlowResult.Empty;

        task.Status |= newStatus;
        
        return task.Updated();
    }

    private MaterialisableFlowResult RewardUnlockedNotification() => 
        FeatureFlagEnabled && PatientJourneyTree.HasReward(JourneyTask.GetId()) ? 
            new PatientJourneyRewardUnlockedNotification(User.FirstName, User.ToEnumerable(), Now, DashboardUrl).ToFlowResult() : 
            MaterialisableFlowResult.Empty;

    private MaterialisableFlowResult BuildIntegrationEvent()
    {
        var newStatus = GetNewStatus();
        
        var source = newStatus switch
        {
            PatientJourneyTaskStatus.PatientCompleted => "patient",
            PatientJourneyTaskStatus.AutoCompleted => "system",
            _ => throw new DomainException($"Unknown status - {newStatus}")
        };

        return new PatientJourneyIntegrationEvent(new PatientJourneyTaskCompletedPayload(User.UniversalId.ToString(), JourneyTask.Title, source), Now).ToFlowResult();
    }
    
    private PatientJourneyTaskStatus GetNewStatus()
    {
        return CompletedBy switch
        {
            JourneyTaskCompletedBy.Patient => PatientJourneyTaskStatus.PatientCompleted,
            JourneyTaskCompletedBy.System => PatientJourneyTaskStatus.AutoCompleted,
            _ => throw new DomainException($"Unknown type {CompletedBy}")
        };
    }
}