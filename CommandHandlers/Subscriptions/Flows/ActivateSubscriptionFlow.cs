using System;
using System.Collections.Generic;
using System.Net;
using WildHealth.Application.Events.Subscriptions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Payment;
using WildHealth.Domain.Models.Timeline.Subscription;
using WildHealth.Shared.Exceptions;
using Subscription = WildHealth.Domain.Entities.Payments.Subscription;

namespace WildHealth.Application.CommandHandlers.Subscriptions.Flows;

public class ActivateSubscriptionFlow : IMaterialisableFlow
{
    private readonly Patient _patient;
    private readonly PaymentPrice _paymentPrice;
    private readonly Subscription _previousSubscription;
    private readonly DateTime? _startDate;
    private readonly DateTime _utcNow;

    public ActivateSubscriptionFlow(
        Patient patient, 
        PaymentPrice paymentPrice, 
        Subscription previousSubscription, 
        DateTime? startDate, 
        DateTime utcNow)
    {
        _patient = patient;
        _paymentPrice = paymentPrice;
        _previousSubscription = previousSubscription;
        _startDate = startDate;
        _utcNow = utcNow;
    }

    public MaterialisableFlowResult Execute()
    {
        if (_patient.CurrentSubscription is not null)
        { 
            throw new DomainException("Patient already has active subscription.");
        }
        
        if (_paymentPrice.GetPrice() != new decimal(0))
        {   
            throw new AppException(HttpStatusCode.BadRequest, "Can't activate subscription with a non zero price.");
        }
        
        var newSubscription = CreateNewSubscription();

        return newSubscription.Added() + 
               new SubscriptionCreatedEvent(_patient) +
               new SubscriptionActivatedEvent(_patient, newSubscription, _previousSubscription) +
               LogCouponCodeChangeTimelineEvent() +
               LogPaymentPlanChangeTimelineEvent() +
               LogPaymentStrategyChangeTimelineEvent() +
               LogSubscriptionDatesChangeTimelineEvent(newSubscription);
    }
    
    private Subscription CreateNewSubscription()
    {
        var (startDate, endDate) = GetSubscriptionPeriod();
        var newSubscriptionPrice = SubscriptionPriceDomain.Create(null, _paymentPrice, null, _utcNow, startDate, false);
        var newSubscription = new Subscription(
            price: newSubscriptionPrice.GetPrice(),
            paymentPrice: _paymentPrice,
            patient: _patient,
            product: null,
            startDate: startDate,
            endDate: endDate,
            promoCodeCoupon: null,
            discounts: newSubscriptionPrice.GetDiscounts(),
            startupFee: 0
        )
        {
            RenewalStrategy = new RenewalStrategy(
                paymentPriceId: _paymentPrice.GetId(),
                promoCodeId: null,
                employerProductId: null
            )
        };
        
        return newSubscription;
    }
    
    private (DateTime, DateTime) GetSubscriptionPeriod()
    {
        var periodInMonths = _paymentPrice.PaymentPeriod.PeriodInMonths;
        var subscriptionStartDate = _startDate ?? _utcNow;
        var subscriptionEndDate = subscriptionStartDate.AddMonths(periodInMonths);
        return (subscriptionStartDate, subscriptionEndDate);
    }
    
    private MaterialisableFlowResult LogPaymentPlanChangeTimelineEvent()
    {
        if (_previousSubscription.PaymentPrice.PaymentPeriod.PaymentPlanId != _paymentPrice.PaymentPeriod.PaymentPlanId)
        {
            var (oldPlan, newPlan) = (_previousSubscription.PaymentPrice.PaymentPeriod.PaymentPlan.DisplayName, _paymentPrice.PaymentPeriod.PaymentPlan.DisplayName);
            return new PaymentPlanReplacedTimelineEvent(_patient.GetId(), _utcNow, new PaymentPlanReplacedTimelineEvent.Data(oldPlan, newPlan)).Added();
        }
        
        return MaterialisableFlowResult.Empty;
    }
    
    private MaterialisableFlowResult LogCouponCodeChangeTimelineEvent()
    {
        if (!string.IsNullOrEmpty(_previousSubscription.PromoCode))
        {
            new PromoCodeRemovedTimelineEvent(
                PatientId: _patient.GetId(), 
                CreatedAt: _utcNow,
                Payload: new PromoCodeRemovedTimelineEvent.Data(_previousSubscription.PromoCode)
            ).Added();
        }

        return MaterialisableFlowResult.Empty;
    }
    
    private MaterialisableFlowResult LogPaymentStrategyChangeTimelineEvent()
    {
        if (_previousSubscription.PaymentPrice.Strategy != _paymentPrice.Strategy)
        {
            var newStrategy = _paymentPrice.Strategy switch
            {
                PaymentStrategy.FullPayment => "yearly",
                PaymentStrategy.PartialPayment => "monthly",
                _ => throw new DomainException($"Unknown payment strategy - {_paymentPrice.Strategy}")
            };
            
            return new PaymentStrategyChangedTimelineEvent(
                PatientId: _patient.GetId(), 
                CreatedAt: _utcNow, 
                Payload: new PaymentStrategyChangedTimelineEvent.Data(newStrategy)
            ).Added();
        }
        
        return MaterialisableFlowResult.Empty;
    }
    
    private IEnumerable<EntityAction> LogSubscriptionDatesChangeTimelineEvent(Subscription newSubscription)
    {
        if (_previousSubscription.StartDate.Date != newSubscription.StartDate.Date)
        {
            var data = new SubscriptionStartDateUpdatedTimelineEvent.Data(_previousSubscription.StartDate.Date, newSubscription.StartDate.Date);
            yield return new SubscriptionStartDateUpdatedTimelineEvent(_previousSubscription.PatientId, _utcNow, data).Added();
        }
        
        if (_previousSubscription.EndDate.Date != newSubscription.EndDate.Date)
        {
            var data = new SubscriptionEndDateUpdatedTimelineEvent.Data(_previousSubscription.EndDate.Date, newSubscription.EndDate.Date);
            yield return new SubscriptionEndDateUpdatedTimelineEvent(_previousSubscription.PatientId, _utcNow, data).Added();
        }
    }
}