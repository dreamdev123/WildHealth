using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Challenges;
using WildHealth.EventSourcing;
using WildHealth.IntegrationEvents.Challenges;
using WildHealth.IntegrationEvents.Challenges.Payloads;

namespace WildHealth.Application.CommandHandlers.Challenges.Flows;

public record CompleteChallengeFlow(PatientChallenge PatientChallenge, DateTime UtcNow, Guid UserUniversalId) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (PatientChallenge.CompletedAt.HasValue) 
            return MaterialisableFlowResult.Empty;
        
        PatientChallenge.CompletedAt = UtcNow;
        
        return PatientChallenge.Updated() + 
               new ChallengeCompleted(PatientChallenge.ChallengeId) +
               new ChallengeIntegrationEvent(new ChallengeCompletedPayload(PatientChallenge.Challenge.Title, UserUniversalId.ToString()), UtcNow);
    }
}

[AggregateEvent("ChallengeCompleted")]
public record ChallengeCompleted(int EntityId) : IAggregateEvent<Nothing>;

public class ChallengeCompletedReducer : IAggregateReducer<Challenge, ChallengeCompleted, Nothing>
{
    public Challenge Reduce(Challenge entity, Nothing _)
    {
        entity.Completed ++;
        return entity;
    }
}