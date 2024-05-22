using System;
using System.Collections.Generic;
using OneOf.Monads;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Challenges;
using WildHealth.Domain.Models.Extensions;
using WildHealth.EventSourcing;
using WildHealth.IntegrationEvents.Challenges;
using WildHealth.IntegrationEvents.Challenges.Payloads;

namespace WildHealth.Application.CommandHandlers.Challenges.Flows;

public record LikeChallengeFlow(Option<PatientChallenge> PatientChallenge, 
    int ChallengeId, 
    int PatientId, 
    string ChallengeTitle, 
    Guid UserUniversalId, 
    DateTime Now) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var (patientChallengeAction, aggregateEvent, isLiked) = PatientChallenge
            .Map(ParticipantLike)
            .ValueOr(NoneParticipantLike());

        return patientChallengeAction + aggregateEvent + IntegrationEvent(isLiked);
    }

    private static (EntityAction, IAggregateEvent, bool) ParticipantLike(PatientChallenge pc)
    {
        pc.Liked = !pc.Liked;
        
        IAggregateEvent challengeAggregateEvent = pc.Liked
            ? new ChallengeLiked(pc.ChallengeId)
            : new ChallengeUnliked(pc.ChallengeId);
        
        return (pc.Updated(), challengeAggregateEvent, pc.Liked);
    }

    private (EntityAction, IAggregateEvent, bool) NoneParticipantLike()
    {
        return
            (new PatientChallenge { PatientId = PatientId, ChallengeId = ChallengeId, IsParticipant = false, Liked = true }.Added(),
            new ChallengeLiked(ChallengeId),
            true);
    }

    private IEnumerable<ChallengeIntegrationEvent> IntegrationEvent(bool liked)
    {
        if (liked)
            yield return new ChallengeIntegrationEvent(new ChallengeLikedPayload(ChallengeTitle, UserUniversalId.ToString()), Now);
    }
}

[AggregateEvent("ChallengeLiked")]
public record ChallengeLiked(int EntityId) : IAggregateEvent<Nothing>;
public class ChallengeLikedReducer : IAggregateReducer<Challenge, ChallengeLiked, Nothing>
{
    public Challenge Reduce(Challenge entity, Nothing _)
    {
        entity.Likes ++;
        return entity;
    }
}

[AggregateEvent("ChallengeUnliked")]
public record ChallengeUnliked(int EntityId) : IAggregateEvent<Nothing>;
public class ChallengeUnlikedReducer : IAggregateReducer<Challenge, ChallengeUnliked, Nothing>
{
    public Challenge Reduce(Challenge entity, Nothing _)
    {
        entity.Likes --;
        return entity;
    }
}

