using System;
using OneOf.Monads;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Challenges;
using WildHealth.Domain.Models.Extensions;
using WildHealth.EventSourcing;
using WildHealth.IntegrationEvents.Challenges;
using WildHealth.IntegrationEvents.Challenges.Payloads;

namespace WildHealth.Application.CommandHandlers.Challenges.Flows;

public record ParticipateInChallengeFlow(Option<PatientChallenge> PatientChallenge,
    Challenge TargetChallenge,
    DateTime UtcNow,
    int PatientId,
    Guid UserUniversalId) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (IsParticipant || // current patient is already participant of the target challenge
            ChallengeNotActive)
            return MaterialisableFlowResult.Empty;
        
        return PatientChallenge.Map(UpdateExistingPatientChallenge).ValueOr(AddNewPatientChallenge());
    }

    /// <summary>
    /// PatientChallenge might already exist when none participant user likes the challenge
    /// </summary>
    private MaterialisableFlowResult UpdateExistingPatientChallenge(PatientChallenge pc)
    {
        pc.IsParticipant = true;

        return pc.Updated() +
            new NewChallengeParticipantAdded(TargetChallenge.GetId()) +
            new ChallengeIntegrationEvent(new ChallengeParticipatedPayload(TargetChallenge.Title, UserUniversalId.ToString()), UtcNow);
    }
    
    private MaterialisableFlowResult AddNewPatientChallenge()
    {
        return
            new PatientChallenge { PatientId = PatientId, ChallengeId = TargetChallenge.GetId(), IsParticipant = true }.Added() +
            new NewChallengeParticipantAdded(TargetChallenge.GetId()) +
            new ChallengeIntegrationEvent(new ChallengeParticipatedPayload(TargetChallenge.Title, UserUniversalId.ToString()), UtcNow);
    }
    
    private bool IsParticipant => PatientChallenge.Map(pc => pc.IsParticipant).ValueOr(false);

    private bool ChallengeNotActive => !IsChallengeActive;

    private bool IsChallengeActive => TargetChallenge.StartDate.Date <= UtcNow.Date && TargetChallenge.EndDate.Date >= UtcNow.Date;
}

[AggregateEvent(name: "NewChallengeParticipantAdded")]
public record NewChallengeParticipantAdded(int EntityId) : IAggregateEvent<Nothing>;

public class NewChallengeParticipantAddedReducer : IAggregateReducer<Challenge, NewChallengeParticipantAdded, Nothing>
{
    public Challenge Reduce(Challenge entity, Nothing _)
    {
        entity.Participants ++;
        return entity;
    }
}