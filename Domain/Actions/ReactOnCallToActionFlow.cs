using System;
using System.Linq;
using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Actions;
using WildHealth.Domain.Enums.Actions;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Actions;
using WildHealth.IntegrationEvents.Actions.Payloads;

namespace WildHealth.Application.Domain.Actions;

public record ReactOnCallToActionFlow(
    Patient Patient,
    CallToAction CallToAction,
    ActionReactionType ReactionType,
    string ReactionDetails,
    DateTime UtcNow): IMaterialisableFlow
{
    private readonly IDictionary<ActionType, ActionReactionType[]> _availableReactions =
        new Dictionary<ActionType, ActionReactionType[]>
        {
            {
                ActionType.StandardPlanUpSell,
                new[]
                {
                    ActionReactionType.Accept,
                    ActionReactionType.Decline
                }
            }
        };

    public MaterialisableFlowResult Execute()
    {
        AssertCanReactOnAction();

        return Act();
    }

    #region private

    private MaterialisableFlowResult Act()
    {
        return (CallToAction.Type, Reaction: ReactionType) switch
        {
            (ActionType.StandardPlanUpSell, ActionReactionType.Decline) => StoreReaction(),
            (ActionType.StandardPlanUpSell, ActionReactionType.Accept) => StoreReaction() + AcceptStandardUpSellOffer(),
            _ => MaterialisableFlowResult.Empty
        };
    }

    private MaterialisableFlowResult StoreReaction()
    {
        CallToAction.Status = ActionStatus.Completed;
        
        var result = new CallToActionResult
        {
            Type = ReactionType,
            Details = ReactionDetails,
            CallToActionId = CallToAction.GetId()
        };

        return MaterialisableFlowResult.Empty + CallToAction.Updated() + result.Added();
    }

    private BaseIntegrationEvent AcceptStandardUpSellOffer() => new CallToActionIntegrationEvent(
        payload: new StandardPlanUpSellPayload(
            planName: CallToAction.GetDataValue<string>(nameof(PaymentPlan.DisplayName)),
            price: CallToAction.GetDataValue<decimal>(nameof(PaymentPrice.Price))),
        user: new UserMetadataModel(Patient.UniversalId.ToString()),
        eventDate: UtcNow
    );

    private void AssertCanReactOnAction()
    {
        if (CallToAction.ExpiresAt < UtcNow)
        {
            throw new DomainException("Alert is expired");
        }
        
        if (CallToAction.Status != ActionStatus.Active)
        {
            throw new DomainException("Call to action hes been reacted already");
        }

        if (!_availableReactions[CallToAction.Type].Contains(ReactionType))
        {
            throw new DomainException("This action is not supported");
        }
    }

    #endregion
}