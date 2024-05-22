using System;
using System.Linq;
using System.Net;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.PromoCodes;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Payments.Flows;

public class GetAlternativePaymentPriceFlow
{
    private readonly PaymentPlan _currentPlan;
    private readonly PaymentPrice _currentPaymentPrice;
    private readonly PaymentStrategy _currentPaymentStrategy;
    private readonly PaymentCoupon _currentPaymentCoupon;
    private readonly PaymentPlan[] _activePlans;
    private readonly PromoCodeCoupon[] _activePromoCodeCoupons;
    private readonly string? _currentPromoCodeCouponCode;
    
    public GetAlternativePaymentPriceFlow(
        PaymentPlan currentPlan,
        PaymentPrice currentPaymentPrice,
        PaymentStrategy currentPaymentStrategy,
        PaymentCoupon currentPaymentCoupon,
        PaymentPlan[] activePlans,
        PromoCodeCoupon[] activePromoCodeCoupons,
        string? currentPromoCodeCouponCode)
    {
        _currentPlan = currentPlan;
        _currentPaymentPrice = currentPaymentPrice;
        _currentPaymentStrategy = currentPaymentStrategy;
        _currentPaymentCoupon = currentPaymentCoupon;
        _activePlans = activePlans;
        _activePromoCodeCoupons = activePromoCodeCoupons;
        _currentPromoCodeCouponCode = currentPromoCodeCouponCode;
    }

    public GetAlternativePaymentPriceFlowResult Execute()
    {
        var paymentPlanCandidate = GetDefaultPaymentPlan(_activePlans);
        
        var alternatePaymentPriceType = GetAltPaymentPriceType(_currentPaymentPrice);

        var equivalentPromoCodeCoupon = GetEquivalentPromoCodeCoupon(_currentPaymentCoupon);

        PaymentPrice? altPrice = null;
        string? couponCode = null;

        // If we are going to standard, then we will not want a promo code, it will drop off
        couponCode = paymentPlanCandidate.Name == PlanNames.Standard
            ? null
            : _currentPromoCodeCouponCode ?? equivalentPromoCodeCoupon;

        altPrice = paymentPlanCandidate
            .PaymentPeriods
            .SelectMany(x => x.Prices)
                
            // This logic below is to handle STANDARD, we want to move people to the 50% off PaymentPrice which is currently NOT active.
            // If the evaluated plan is NOT STANDARD, then we want to get the active PaymentPrice
            .Where(x => paymentPlanCandidate.Name == PlanNames.Standard ? 
                !x.IsActive && x.Strategy == _currentPaymentStrategy : 
                x.IsActive && x.Strategy == _currentPaymentStrategy)
            .FirstOrDefault(x => x.Type == alternatePaymentPriceType);
                
        if (altPrice is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Can't turn on/off insurance subscription. No alternative price.");
        }

        return new GetAlternativePaymentPriceFlowResult(altPrice, couponCode);
    }

    
    private PaymentPlan GetDefaultPaymentPlan(PaymentPlan[] acivePlans)
    {
        // This logic excludes all
        // 1. activation plans
        // 2. non-default plans (i.e. Precision coaching and wild health light)
        // 3. non-perpetual subscription plans (i.e. PRECISION_CARE_PACKAGE)
        // Updated 8/17
        // Want to update this logic to pull STANDARD 50% off
        var paymentPlan = acivePlans
            .FirstOrDefault(o => 
                o.Type == PaymentPlanType.Default && 
                o.Name == PlanNames.Standard &&
                !o.CanBeActivated);
        
        if (paymentPlan is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Cannot locate default payment plan to switch to");
        }

        return paymentPlan;
    }
    
    private string? GetEquivalentPromoCodeCoupon(PaymentCoupon pc)
    {
        if (pc is null)
        {
            return null;
        }

        var discountAmount = pc.Detail?.Split("% off").FirstOrDefault();

        if (discountAmount is null)
        {
            return null;
        }

        var promoCodeCoupon = _activePromoCodeCoupons
            .Where(o => o.ExpirationDate == null || o.ExpirationDate > DateTime.UtcNow)
            .Where(o => o.DiscountType == DiscountType.Percentage)
            .FirstOrDefault(o => o.Discount == Convert.ToInt32(discountAmount));

        return promoCodeCoupon?.Code;
    }
    
    private PaymentPriceType GetAltPaymentPriceType(PaymentPrice currentPaymentPrice)
    {
        return currentPaymentPrice.Type switch
        {
            PaymentPriceType.Default => PaymentPriceType.Insurance,
            PaymentPriceType.Insurance => PaymentPriceType.Default,
            PaymentPriceType.PromoCode => PaymentPriceType.Insurance,
            PaymentPriceType.InsurancePromoCode => PaymentPriceType.Default,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private bool CanSwitchToInsurance(PaymentPlan paymentPlan)
    {
        return
            paymentPlan.IsActive &&
            paymentPlan.Type == PaymentPlanType.Default &&
            !paymentPlan.CanBeActivated && paymentPlan.IsSingle;
    }
}

public record GetAlternativePaymentPriceFlowResult(
    PaymentPrice AlternatePaymentPrice,
    string? CouponCode)
{
    public static GetAlternativePaymentPriceFlowResult Empty => new GetAlternativePaymentPriceFlowResult(new PaymentPrice(), null);
}