using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Models.Subscriptions;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.RenewalWorkflow;
using WildHealth.IntegrationEvents.RenewalWorkflow.Payloads;

namespace WildHealth.Application.CommandHandlers.Subscriptions.Flows;

public class OverwriteSubscriptionFlow : IMaterialisableFlow
{
    private readonly Subscription _subscription;
    private readonly PaymentPrice _altFullPaymentPrice;
    private readonly PaymentPrice _altPartialPaymentPrice;
    private readonly decimal _minPriceCriteria;
    private readonly int _noticePeriod;
    private readonly DateTime _now;

    public OverwriteSubscriptionFlow(
        Subscription subscription,
        PaymentPrice altFullPaymentPrice, 
        PaymentPrice altPartialPaymentPrice, 
        decimal minPriceCriteria, 
        int noticePeriod, 
        DateTime now)
    {
        _subscription = subscription;
        _altFullPaymentPrice = altFullPaymentPrice;
        _altPartialPaymentPrice = altPartialPaymentPrice;
        _minPriceCriteria = minPriceCriteria;
        _noticePeriod = noticePeriod;
        _now = now;
    }

    public MaterialisableFlowResult Execute()
    {
        if (!SubscriptionCanBeRenewed())
        {
            return MaterialisableFlowResult.Empty;
        }

        if (PaymentPriceShouldBeOverwritten())
        {
            OverwritePaymentPrice();
        }
        else
        {
            return MaterialisableFlowResult.Empty;
        }

        return _subscription.Updated() + RaiseIntegrationEvent();
    }

    private bool SubscriptionCanBeRenewed()
    {
        if (!_subscription.Renewable)
        {
            return false;
        }

        if (_subscription.CancellationRequest is not null)
        {
            return _subscription.EndDate < _subscription.CancellationRequest.Date;
        }
        
        return true;
    }
    
    private bool PaymentPriceShouldBeOverwritten()
    {
        // Skip subscriptions which were already processed by this flow
        if (_subscription.RenewalStrategy?.Source == RenewalStrategySource.OverwriteSubscriptionFlow)
        {
            return false;
        }

        if (!_subscription.ShouldBeMovedToStandardPlan())
        {
            return false;
        }
        
        return _subscription.PaymentPrice.Strategy switch
        {
            PaymentStrategy.PartialPayment => _subscription.Price < _minPriceCriteria,
            PaymentStrategy.FullPayment => _subscription.Price / _subscription.PaymentPrice.PaymentPeriod.PeriodInMonths < _minPriceCriteria,
            _ => throw new ArgumentException(nameof(PaymentStrategy))
        };
    }

    private void OverwritePaymentPrice()
    {
        var altPaymentPrice = _subscription.PaymentStrategy switch
        {
            PaymentStrategy.FullPayment => _altFullPaymentPrice,
            PaymentStrategy.PartialPayment => _altPartialPaymentPrice,
            _ => throw new ArgumentException(nameof(PaymentStrategy))
        };
        
        if (_subscription.RenewalStrategy is null)
        {
            _subscription.RenewalStrategy = CreateRenewalStrategy(altPaymentPrice.GetId());
        }
        else
        {
            _subscription.RenewalStrategy.PaymentPriceId = altPaymentPrice.GetId();
            _subscription.RenewalStrategy.PromoCodeId = null;
            _subscription.RenewalStrategy.Source = RenewalStrategySource.OverwriteSubscriptionFlow;
        }
    }

    private RenewalStrategy CreateRenewalStrategy(int paymentPriceId)
    {
        return new RenewalStrategy(
            paymentPriceId: paymentPriceId,
            promoCodeId: null,
            employerProductId: _subscription.EmployerProduct is null
                ? null
                : !_subscription.EmployerProduct.IsLimited
                    ? _subscription.EmployerProduct.GetId()
                    : null
        )
        {
            Source = RenewalStrategySource.OverwriteSubscriptionFlow
        };

    }
    
    private BaseIntegrationEvent RaiseIntegrationEvent() => new RenewalWorkflowIntegrationEvent(
        user: new UserMetadataModel(_subscription.Patient.User.UniversalId.ToString()),
        payload: new CreatedRenewalWorkflowPayload(
            daysUntilRenewal: _noticePeriod, 
            paymentStrategy: _subscription.PaymentPrice.Strategy.ToString(),
            isInsurance: _subscription.GetSubscriptionType() == SubscriptionType.Insurance),
        eventDate: _now
    );
}