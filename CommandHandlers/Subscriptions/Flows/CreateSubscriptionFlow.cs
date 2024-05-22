using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.PromoCodes;
using WildHealth.Domain.Models.Payment;

namespace WildHealth.Application.CommandHandlers.Subscriptions.Flows;

public class CreateSubscriptionFlow : IMaterialisableFlow
{
    private readonly Patient _patient;
    private readonly PaymentPrice _paymentPrice;
    private readonly DateTime? _startDate;
    private readonly DateTime? _endDate;
    private readonly EmployerProduct? _employerProduct;
    private readonly DateTime _utcNow;
    private readonly PromoCodeCoupon? _coupon;
    private readonly bool _chargeStartupFee;

    public CreateSubscriptionFlow(
        Patient patient,
        PaymentPrice paymentPrice,
        EmployerProduct? employerProduct,
        PromoCodeCoupon? coupon,
        bool chargeStartupFee,
        DateTime utcNow,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        _patient = patient;
        _paymentPrice = paymentPrice;
        _employerProduct = employerProduct;
        _coupon = coupon;
        _startDate = startDate;
        _endDate = endDate;
        _utcNow = utcNow;
        _chargeStartupFee = chargeStartupFee;
    }

    public MaterialisableFlowResult Execute()
    {
        var subscriptionPrice = SubscriptionPriceDomain.Create(
            _coupon, 
            _paymentPrice, 
            _employerProduct, 
            _utcNow,
            _startDate,
            _chargeStartupFee);
        
        var couponDomain = PromoCodeDomain.Create(_coupon, _utcNow);
        couponDomain.ThrowIfNotUsable(_paymentPrice.Type);
        
        var (startDate, endDate) = GetDates();
        
        var subscription = new Subscription(
            price: subscriptionPrice.GetPrice(),
            paymentPrice: _paymentPrice,
            patient: _patient,
            product: _employerProduct,
            startDate: startDate,
            endDate: endDate,
            promoCodeCoupon: _coupon,
            discounts: subscriptionPrice.GetDiscounts(),
            startupFee: subscriptionPrice.GetStartupFee()
        );

        subscription.RenewalStrategy = new RenewalStrategy(
            paymentPriceId: _paymentPrice.GetId(),
            promoCodeId: _coupon?.GetId(),
            employerProductId: _employerProduct is null 
                ? null 
                : !_employerProduct.IsLimited 
                    ? _employerProduct.GetId() 
                    : null
        );
        
        return new MaterialisableFlowResult(subscription.Added());
    }

    private (DateTime, DateTime endDate) GetDates()
    {
        var subscriptionStartDate = _startDate ?? _utcNow;
        var subscriptionEndDate = _endDate ?? _utcNow.AddMonths(_paymentPrice.PaymentPeriod.PeriodInMonths);
        return (subscriptionStartDate, subscriptionEndDate);
    }
}