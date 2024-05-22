using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Exceptions;
using WildHealth.IntegrationEvents.PatientEngagements;
using WildHealth.IntegrationEvents.PatientEngagements.Payloads;

namespace WildHealth.Application.Domain.PatientEngagements.Flows;

public record CompleteHealthCoachEngagementFlow(PatientEngagement? Engagement, User? CompletedBy, DateTime Now) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (Engagement is null)
        {
            throw new DomainException("Task does not exist");
        }
        
        if (Engagement!.Completed())
        {
            throw new DomainException("Task already completed");
        }

        Engagement.Status = PatientEngagementStatus.Completed;
        Engagement.CompletedBy = CompletedBy?.GetId() ?? null;

        return Engagement.Updated() + PatientEngagementIntegrationEvent();
    }

    private MaterialisableFlowResult PatientEngagementIntegrationEvent()
    {
        if (Engagement is null || string.IsNullOrEmpty(Engagement.EngagementCriteria.AnalyticsEvent))
            return MaterialisableFlowResult.Empty;
        
        var payload = new PatientEngagementCompletedPayload(Engagement.EngagementCriteria.AnalyticsEvent, Engagement.Patient.User.UniversalId.ToString());
        return new PatientEngagementIntegrationEvent(payload, Now).ToFlowResult();
    }
}