using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using WildHealth.Application.Events.Subscriptions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.PromoCodes;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Models.Payment;
using WildHealth.Domain.Models.Subscriptions;
using WildHealth.Integration.Models.Subscriptions;

namespace WildHealth.Application.CommandHandlers.Subscriptions.Flows;

public record RenewSubscriptionFlow(Subscription CurrentSubscription,
    SubscriptionIntegrationModel? IntegrationSubscription,
    Patient Patient,
    PaymentPrice NewPaymentPrice,
    EmployerProduct? EmployerProduct,
    PromoCodeCoupon? Coupon,
    IntegrationVendor IntegrationVendor,
    DateTime UtcNow,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? PatientProfileLink = null) : BaseSubscriptionLoggingFlow, IMaterialisableFlow
{
    private readonly string[] _activeStatuses =
    {
        "trialing", 
        "active", 
        "past_due" // When stripe tries to charge and fails before Clarity renewal (at 4AM UTC) it's still should be treated as Active 
    };
    
    public MaterialisableFlowResult Execute()
    {
        var newSubscription = CreateNewSubscription();

        CurrentSubscription.Cancel(UtcNow, CancellationReasonType.Renewed, "Automatic renew subscription");

        if (IsIntegrationSubscriptionActive)
            LinkNewSubscriptionWithExistingIntegrationSubscription(newSubscription);

        return CurrentSubscription.Updated() +
               newSubscription.Added() +
               FirePremiumRenewalAlert() +
               FireSubscriptionStateChangedEvent(newSubscription) +
               new SubscriptionCreatedEvent(Patient) +
               LogCouponCodeChangeTimelineEvent(CurrentSubscription, newSubscription, UtcNow, Coupon) +
               LogPaymentPlanChangeTimelineEvent(CurrentSubscription, NewPaymentPrice, UtcNow) +
               LogPaymentStrategyChangeTimelineEvent(CurrentSubscription, NewPaymentPrice, UtcNow) +
               LogSubscriptionDatesChangeTimelineEvent(CurrentSubscription, newSubscription, UtcNow);
    }

    private MaterialisableFlowResult FirePremiumRenewalAlert()
    {
        return CurrentSubscription.IsPremium() ?
            new PremiumSubscriptionRenewedNotification(PatientProfileLink, CurrentSubscription.PracticeId!.Value, UtcNow).ToFlowResult() :
            MaterialisableFlowResult.Empty;
    }

    private void LinkNewSubscriptionWithExistingIntegrationSubscription(Subscription newSubscription)
    {
        // Find current subscription integration and assign it to the new subscription 
        var currentSubscriptionIntegration = CurrentSubscription.Integrations.First(i =>
            i.Integration.Vendor == IntegrationVendor &&
            i.Integration.Purpose == IntegrationPurposes.Payment.Id); // subscription in Stripe

        newSubscription.Integrations.Add(new SubscriptionIntegration
        {
            IntegrationId = currentSubscriptionIntegration.IntegrationId,
            Subscription = newSubscription,
            Integration = currentSubscriptionIntegration.Integration
        });
    }

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

    private IEnumerable<INotification> FireSubscriptionStateChangedEvent(Subscription newSubscription)
    {
        if (!IsIntegrationSubscriptionActive)
        {
            // If stripe subscription is not active for some reason we fire this event and create new subscription in stripe
            yield return new IntegrationSubscriptionCanceledEvent(Patient, newSubscription, NewPaymentPrice, EmployerProduct, Coupon, IntegrationVendor);
        }
        else if (Math.Round(newSubscription.Price, 2) != Math.Round(CurrentSubscription.Price, 2) || newSubscription.StartDate.Date != CurrentSubscription.StartDate.Date)
        {
            var newSubscriptionPrice = SubscriptionPriceDomain.Create(Coupon, NewPaymentPrice, EmployerProduct, UtcNow, newSubscription.StartDate, false);
            var integrationSubscriptionId = newSubscription.Integrations.First().Integration.Value;
            yield return new SubscriptionPriceChangedEvent(CurrentSubscription.PracticeId!.Value, integrationSubscriptionId, newSubscriptionPrice);
        }
    }

    private (DateTime, DateTime) GetSubscriptionPeriod()
    {
        var subscriptionStartDate = StartDate ?? UtcNow;
        var subscriptionEndDate = EndDate ?? UtcNow.AddMonths(NewPaymentPrice.PaymentPeriod.PeriodInMonths);
        return (subscriptionStartDate, subscriptionEndDate);
    }

    private bool IsIntegrationSubscriptionActive => _activeStatuses.Contains(IntegrationSubscription?.Status.ToLower());
}