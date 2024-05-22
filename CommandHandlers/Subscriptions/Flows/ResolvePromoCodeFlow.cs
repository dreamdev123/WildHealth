using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.PromoCodes;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Payment;

namespace WildHealth.Application.CommandHandlers.Subscriptions.Flows;

public class ResolvePromoCodeFlow
{
    private readonly PromoCodeCoupon? _subscriptionPromoCode;
    private readonly List<PaymentPrice> _paymentPrices;
    private readonly Subscription _subscription;
    private readonly PromoCodeCoupon? _oldPromoCodeReplacement;
    private readonly RenewalStrategy? _renewalStrategy;
    private readonly DateTime _now;

    public ResolvePromoCodeFlow(Subscription subscription,
        List<PaymentPrice> paymentPrices,
        PromoCodeCoupon? subscriptionPromoCode,
        PromoCodeCoupon? oldPromoCodeReplacement,
        RenewalStrategy? renewalStrategy,
        DateTime now)
    {
        _subscription = subscription;
        _paymentPrices = paymentPrices;
        _subscriptionPromoCode = subscriptionPromoCode;
        _oldPromoCodeReplacement = oldPromoCodeReplacement;
        _renewalStrategy = renewalStrategy;
        _now = now;
    }
    
    public ResolvePromoCodeFlowResult Execute()
    {
        if (_renewalStrategy is not null)
        {
            var paymentPrice = _renewalStrategy.PaymentPrice;
            
            var promoCode = _renewalStrategy.PromoCode;

            if (promoCode is null)
            {
                return new ResolvePromoCodeFlowResult(paymentPrice.GetId(), null);
            }
            
            var code = GetOngoingCode(promoCode, paymentPrice.Type);
            
            return new ResolvePromoCodeFlowResult(paymentPrice.GetId(), code);
        }
        
        if (_subscriptionPromoCode is not null)
        {
            // New Promo Code workflow is used for the current Subscription so just roll it over to the new one
            var code = GetOngoingCode(_subscriptionPromoCode, _subscription.PaymentPrice.Type);

            return new ResolvePromoCodeFlowResult(_subscription.PaymentPrice.GetId(), code);
        }
        
        if (_subscription.PaymentPrice.Type is PaymentPriceType.Default or PaymentPriceType.Insurance
            && _subscriptionPromoCode is null)
        {
            // No Promo Code is used
            return new ResolvePromoCodeFlowResult(_subscription.PaymentPrice.GetId(), null);
        }

        if (_subscription.PaymentPrice.Type is PaymentPriceType.PromoCode or PaymentPriceType.InsurancePromoCode)
        {
            // Subscription uses old Promo Code discount so we replace it with the equivalent new one if exists
            var defaultPaymentPrice = GetDefaultPrice(_subscription.PaymentPrice.Type, _subscription.PaymentPrice.Strategy);
            if (defaultPaymentPrice is null)
            {
                throw new DomainException($"Tried to migrate Coupon Codes but no Default Payment Price found for {_subscription.PaymentPrice.GetId()}. Subscription: {_subscription.GetId()}");
            }

            var code = GetOngoingCode(_oldPromoCodeReplacement, defaultPaymentPrice.Type);
            
            return new ResolvePromoCodeFlowResult(defaultPaymentPrice.GetId(), code);
        }
        
        return new ResolvePromoCodeFlowResult(_subscription.PaymentPrice.GetId(), _subscription.PaymentPrice.PaymentCoupon?.Code);
    }

    private string? GetOngoingCode(PromoCodeCoupon? promoCodeCoupon, PaymentPriceType paymentPriceType)
    {
        var domain = PromoCodeDomain.Create(promoCodeCoupon, _now);

        var isOngoing = domain.IsActive && 
                        !domain.IsExpired &&
                        domain.HasPaymentPlan(_subscription.PaymentPrice.PaymentPeriod.PaymentPlanId);

        // if InsurancePromoCode payment price then we check if the PromoCode supports it.
        var isApplyForInsurance = paymentPriceType == PaymentPriceType.Insurance ? 
            domain.IsAppliedForInsurance : true;
        
        return isOngoing && isApplyForInsurance ? domain.Code : null;
    }

    private PaymentPrice? GetDefaultPrice(PaymentPriceType type, PaymentStrategy strategy)
    {
        var defaultType = GetDefaultType(type);
        
        var defaultPrices = _paymentPrices.Where(x => 
            x.Type == defaultType && 
            x.Strategy == strategy
        ).ToArray();

        return defaultPrices.Any(x => x.IsActive)
            ? defaultPrices.FirstOrDefault(x => x.IsActive)
            : defaultPrices.FirstOrDefault();
    }

    private PaymentPriceType GetDefaultType(PaymentPriceType type)
    {
        return type switch
        {
            PaymentPriceType.PromoCode => PaymentPriceType.Default,
            PaymentPriceType.InsurancePromoCode => PaymentPriceType.Insurance,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"The price type of {type} doesn't have corresponding default option")
        };
    }
}

public record ResolvePromoCodeFlowResult(int PaymentPriceId, string? Code);
