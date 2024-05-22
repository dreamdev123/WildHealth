using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Application.Services.PaymentPrices;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Application.Services.PromoCodes;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Models.Payment;
using WildHealth.Integration.Factories.IntegrationServiceFactory;

namespace WildHealth.Application.CommandHandlers.Subscriptions;

public class RefreshSubscriptionIntegrationCommandHandler : IRequestHandler<RefreshSubscriptionIntegrationCommand>
{
    private readonly IPaymentService _paymentService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPromoCodeCouponsService _promoCodeCouponsService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPaymentPriceService _paymentPriceService;
    private readonly IPaymentPlansService _paymentPlansService;
    private readonly IIntegrationServiceFactory _integrationServiceFactory;

    public RefreshSubscriptionIntegrationCommandHandler(
        IPaymentService paymentService, 
        ISubscriptionService subscriptionService,
        IPromoCodeCouponsService promoCodeCouponsService,
        IDateTimeProvider dateTimeProvider,
        IPaymentPriceService paymentPriceService,
        IPaymentPlansService paymentPlansService,
        IIntegrationServiceFactory integrationServiceFactory)
    {
        _paymentService = paymentService;
        _subscriptionService = subscriptionService;
        _promoCodeCouponsService = promoCodeCouponsService;
        _dateTimeProvider = dateTimeProvider;
        _paymentPriceService = paymentPriceService;
        _paymentPlansService = paymentPlansService;
        _integrationServiceFactory = integrationServiceFactory;
    }

    public async Task Handle(RefreshSubscriptionIntegrationCommand command, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow();
        var subscription = await _subscriptionService.GetAsync(command.SubscriptionId);
        var employerProduct = subscription.EmployerProduct;
        var paymentInfo = await GetCouponCodeAndPaymentPrice(subscription, utcNow);
        var paymentPrice = await _paymentPlansService.GetPaymentPriceByIdAsync(paymentInfo.PaymentPriceId);
        var coupon = await _promoCodeCouponsService.GetAsync(paymentInfo.Code, subscription.PracticeId!.Value);
        
        var subscriptionPrice = SubscriptionPriceDomain.Create(coupon, paymentPrice, employerProduct, utcNow, subscription.StartDate, false);
        var integrationSubscriptionId = subscription.Integrations.First().Integration.Value;
        
        await _paymentService.UpdateSubscriptionPriceAsync(subscription.Patient.User.PracticeId, integrationSubscriptionId, subscriptionPrice);
    }
    

    private async Task<ResolvePromoCodeFlowResult> GetCouponCodeAndPaymentPrice(Subscription currentSubscription, DateTime now)
    {
        var paymentPrices =
            await _paymentPriceService.GetByPeriodIdAsync(currentSubscription.PaymentPrice.PaymentPeriodId);
            
        // Only new promo codes are directly linked with subscriptions
        var subscriptionPromoCode = currentSubscription.PromoCodeCouponId.HasValue
            ? await _promoCodeCouponsService.GetByIdAsync(currentSubscription.PromoCodeCouponId.Value)
            : null;
            
        // When old promo code discount is used then find the equivalent new promo code replacement for it 
        var oldPromoCodeReplacement = await _promoCodeCouponsService.GetAsync(
            currentSubscription.PaymentPrice.PaymentCoupon?.Code,
            currentSubscription.PracticeId!.Value);
            
        var flow = new ResolvePromoCodeFlow(
            subscription: currentSubscription,
            paymentPrices: paymentPrices.ToList(),
            subscriptionPromoCode: subscriptionPromoCode,
            oldPromoCodeReplacement: oldPromoCodeReplacement,
            renewalStrategy: currentSubscription.RenewalStrategy,
            now: now
        );

        return flow.Execute();
    }
}