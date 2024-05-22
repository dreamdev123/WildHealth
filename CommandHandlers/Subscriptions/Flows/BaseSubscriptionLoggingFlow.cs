using System;
using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.PromoCodes;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Timeline.Subscription;

namespace WildHealth.Application.CommandHandlers.Subscriptions.Flows;

public abstract record BaseSubscriptionLoggingFlow
{
    protected virtual MaterialisableFlowResult LogCouponCodeChangeTimelineEvent(Subscription recentSubscription, Subscription newSubscription, DateTime utcNow, PromoCodeCoupon? coupon)
    {
        return (recentSubscription.PromoCodeCouponId, newSubscription.PromoCodeCouponId) switch
        {
            // unchanged
            (null, null) => MaterialisableFlowResult.Empty,
            // added
            (null, _) => new PromoCodeAddedTimelineEvent(recentSubscription.PatientId, utcNow, new PromoCodeAddedTimelineEvent.Data(coupon?.Code ?? "")).Added(),
            // removed
            (_, null) => new PromoCodeRemovedTimelineEvent(recentSubscription.PatientId, utcNow, new PromoCodeRemovedTimelineEvent.Data(coupon?.Code ?? "")).Added(),
            // unchanged
            (_, _) => MaterialisableFlowResult.Empty
        };
    }
    
    protected virtual MaterialisableFlowResult LogPaymentPlanChangeTimelineEvent(Subscription recentSubscription, PaymentPrice newPaymentPrice, DateTime utcNow)
    {
        if (recentSubscription.PaymentPrice.PaymentPeriod.PaymentPlanId != newPaymentPrice.PaymentPeriod.PaymentPlanId)
        {
            var (oldPlan, newPlan) = (recentSubscription.PaymentPrice.PaymentPeriod.PaymentPlan.DisplayName, newPaymentPrice.PaymentPeriod.PaymentPlan.DisplayName);
            return new PaymentPlanReplacedTimelineEvent(recentSubscription.PatientId, utcNow, new PaymentPlanReplacedTimelineEvent.Data(oldPlan, newPlan)).Added();
        }
        
        return MaterialisableFlowResult.Empty;
    }
    
    protected virtual MaterialisableFlowResult LogPaymentStrategyChangeTimelineEvent(Subscription recentSubscription, PaymentPrice newPaymentPrice, DateTime utcNow)
    {
        if (recentSubscription.PaymentPrice.Strategy != newPaymentPrice.Strategy)
        {
            var newStrategy = newPaymentPrice.Strategy switch
            {
                PaymentStrategy.FullPayment => "yearly",
                PaymentStrategy.PartialPayment => "monthly",
                _ => throw new DomainException($"Unknown payment strategy - {newPaymentPrice.Strategy}")
            };
            
            return new PaymentStrategyChangedTimelineEvent(recentSubscription.PatientId, utcNow, new PaymentStrategyChangedTimelineEvent.Data(newStrategy)).Added();
        }
        
        return MaterialisableFlowResult.Empty;
    }
    
    protected virtual IEnumerable<EntityAction> LogSubscriptionDatesChangeTimelineEvent(Subscription recentSubscription, Subscription newSubscription, DateTime utcNow)
    {
        if (recentSubscription.StartDate.Date != newSubscription.StartDate.Date)
        {
            var data = new SubscriptionStartDateUpdatedTimelineEvent.Data(recentSubscription.StartDate.Date, newSubscription.StartDate.Date);
            yield return new SubscriptionStartDateUpdatedTimelineEvent(recentSubscription.PatientId, utcNow, data).Added();
        }
        
        if (recentSubscription.EndDate.Date != newSubscription.EndDate.Date)
        {
            var data = new SubscriptionEndDateUpdatedTimelineEvent.Data(recentSubscription.EndDate.Date, newSubscription.EndDate.Date);
            yield return new SubscriptionEndDateUpdatedTimelineEvent(recentSubscription.PatientId, utcNow, data).Added();
        }
    }
}