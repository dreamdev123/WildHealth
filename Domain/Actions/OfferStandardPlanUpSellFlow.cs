using System;
using System.Linq;
using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Actions;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Actions;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Models.Subscriptions;

namespace WildHealth.Application.Domain.Actions;

public record OfferStandardPlanUpSellFlow(
    Subscription Subscription,
    PaymentPrice AltFullPaymentPrice, 
    PaymentPrice AltPartialPaymentPrice, 
    decimal MinPriceCriteria,
    CallToAction[] CallToActions): IMaterialisableFlow
{
    private const ActionType ActionType = WildHealth.Domain.Enums.Actions.ActionType.StandardPlanUpSell;

    public MaterialisableFlowResult Execute()
    {
        if (!SubscriptionCanBeRenewed())
        {
            return MaterialisableFlowResult.Empty;
        }

        if (StandardPlanUpSellCanBeOffered())
        {
            OfferedStandardPlanUpSell();
        }
        else
        {
            return MaterialisableFlowResult.Empty;
        }

        return OfferedStandardPlanUpSell();
    }

    private bool SubscriptionCanBeRenewed()
    {
        if (!Subscription.Renewable)
        {
            return false;
        }

        if (Subscription.CancellationRequest is not null)
        {
            return Subscription.EndDate < Subscription.CancellationRequest.Date;
        }
        
        return true;
    }
    
    private bool StandardPlanUpSellCanBeOffered()
    {
        // Skip patients which already received offer
        if (CallToActions.Any(x => x.Type == ActionType))
        {
            return false;
        }
        
        if (!Subscription.ShouldBeMovedToStandardPlan())
        {
            return false;
        }
        
        return Subscription.PaymentPrice.Strategy switch
        {
            PaymentStrategy.PartialPayment => Subscription.Price < MinPriceCriteria,
            PaymentStrategy.FullPayment => Subscription.Price / Subscription.PaymentPrice.PaymentPeriod.PeriodInMonths < MinPriceCriteria,
            _ => throw new ArgumentException(nameof(PaymentStrategy))
        };
    }

    private MaterialisableFlowResult OfferedStandardPlanUpSell()
    {
        var altPaymentPrice = Subscription.PaymentStrategy switch
        {
            PaymentStrategy.FullPayment => AltFullPaymentPrice,
            PaymentStrategy.PartialPayment => AltPartialPaymentPrice,
            _ => throw new ArgumentException(nameof(PaymentStrategy))
        };

        return MaterialisableFlowResult.Empty + new CallToActionSuccessTriggerEvent(
            PatientId: Subscription.PatientId,
            Type: ActionType,
            ExpiresAt: Subscription.EndDate.AddMonths(-1),
            Reactions: new[]
            {
                ActionReactionType.Accept,
                ActionReactionType.Decline
            },
            Data: new Dictionary<string, string>
            {
                { nameof(PaymentPrice.Price), altPaymentPrice.Price.ToString() },
                { nameof(PaymentPlan.DisplayName), altPaymentPrice.PaymentPeriod.PaymentPlan.DisplayName }
            }
        );
    }
}