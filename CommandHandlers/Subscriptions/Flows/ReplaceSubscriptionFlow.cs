using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Domain.PaymentIssues;
using WildHealth.Application.Events.Subscriptions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.PromoCodes;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Payment;
using WildHealth.Domain.Models.Timeline.Subscription;
using Subscription = WildHealth.Domain.Entities.Payments.Subscription;

namespace WildHealth.Application.CommandHandlers.Subscriptions.Flows;

public record ReplaceSubscriptionFlow(
    Patient Patient, 
    PaymentPrice NewPaymentPrice, 
    EmployerProduct? EmployerProduct, 
    PromoCodeCoupon? Coupon, 
    Subscription RecentSubscription, 
    PaymentIssue[] PaymentIssues,
    Founder? Founder, 
    bool IsFounderPlan,
    DateTime? StartDate, 
    DateTime UtcNow) : BaseSubscriptionLoggingFlow, IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (IsFounderPlan)
        {
            AssertFounderIsSelected(Founder);
        }
        
        if (!IsCurrentSubscriptionCanBeReplaced(RecentSubscription))
        {
            throw new DomainException("Can't replace current subscription.");
        }
        
        var newSubscription = CreateNewSubscription();

        return newSubscription.Added() 
            + new SubscriptionCreatedEvent(Patient)
            + CancelSubscriptionPaymentIssues()
            + LogCouponCodeChangeTimelineEvent(RecentSubscription, newSubscription, UtcNow, Coupon)
            + LogPaymentPlanChangeTimelineEvent(RecentSubscription, NewPaymentPrice, UtcNow)
            + LogPaymentStrategyChangeTimelineEvent(RecentSubscription, NewPaymentPrice, UtcNow)
            + LogSubscriptionDatesChangeTimelineEvent(RecentSubscription, newSubscription, UtcNow);
    }
    
    #region private
    
    private Subscription CreateNewSubscription()
    {
        var (startDate, endDate) = GetSubscriptionPeriod();
        var newSubscriptionPrice = SubscriptionPriceDomain.Create(Coupon, NewPaymentPrice, EmployerProduct, UtcNow, startDate, false);
        var newSubscription = new Subscription(
            price: newSubscriptionPrice.GetPrice(),
            paymentPrice: NewPaymentPrice,
            patient: Patient,
            product: EmployerProduct,
            startDate: startDate,
            endDate: endDate,
            promoCodeCoupon: Coupon,
            discounts: newSubscriptionPrice.GetDiscounts(),
            startupFee: 0
        )
        {
            RenewalStrategy = new RenewalStrategy(
                paymentPriceId: NewPaymentPrice.GetId(),
                promoCodeId: Coupon?.GetId(),
                employerProductId: EmployerProduct is null
                    ? null
                    : !EmployerProduct.IsLimited
                        ? EmployerProduct.GetId()
                        : null
            ),
        };

        return newSubscription;
    }
    
    private MaterialisableFlowResult CancelSubscriptionPaymentIssues()
    {
        return PaymentIssues
            .Where(x => x.Type == PaymentIssueType.Subscription)
            .Select(x => x.UserCancel())
            .Aggregate(MaterialisableFlowResult.Empty, (cur, acc) => cur + acc);
    }

    private void AssertFounderIsSelected(Founder? founder)
    {
        if (founder is null)
        {
            throw new DomainException("Founder is not selected.");
        }
    }
    
    private bool IsCurrentSubscriptionCanBeReplaced(Subscription? subscription)
    {
        if (subscription is null || !subscription.IsActive)
        {
            return true;
        }
        
        return subscription.GetStatus() == SubscriptionStatus.Active && subscription.CanBeReplaced();
    }
    
    private (DateTime, DateTime) GetSubscriptionPeriod()
    {
        var periodInMonths = NewPaymentPrice.PaymentPeriod.PeriodInMonths;
        var subscriptionStartDate = StartDate ?? UtcNow;
        var subscriptionEndDate = subscriptionStartDate.AddMonths(periodInMonths);
        return (subscriptionStartDate, subscriptionEndDate);
    }

    #endregion
}