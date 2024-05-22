using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Services.PaymentPlans;
using MediatR;
using WildHealth.Application.Services.PromoCodes;
using WildHealth.Common.Models.Payments;
using WildHealth.Domain.Models.Payment;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Payments
{
    public class RecommendSubscriptionCommandHandler : IRequestHandler<RecommendSubscriptionCommand, RecommendPaymentPriceModel>
    {
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly IPromoCodeCouponsService _promoCodeCouponsService;
        
        public RecommendSubscriptionCommandHandler(
            IPaymentPlansService paymentPlansService, 
            IPromoCodeCouponsService promoCodeCouponsService)
        {
            _paymentPlansService = paymentPlansService;
            _promoCodeCouponsService = promoCodeCouponsService;
        }

        public async Task<RecommendPaymentPriceModel> Handle(RecommendSubscriptionCommand command, CancellationToken cancellationToken)
        {
            var result = await GetPaymentPrice(command);

            return new RecommendPaymentPriceModel
            {
                PlanName = result.PaymentPrice.PaymentPeriod.PaymentPlan.Name,
                PaymentStrategy = result.PaymentPrice.Strategy,
                PaymentPriceType = result.PaymentPrice.Type,
                IsInsurance = command.IsInsurance,
                CouponCode = command.CouponCode,
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                PaymentDescription = result.PaymentDescription(),
                InclusionsDescription = result.PaymentPrice.PaymentPeriod.InclusionsDescription()
            };
        }

        private async Task<PaymentPriceDomain> GetPaymentPrice(RecommendSubscriptionCommand command)
        {
            var price = await _paymentPlansService.GetPriceV2(
                planName: command.PlanName,
                paymentStrategy: command.PaymentStrategy,
                isInsurance: command.IsInsurance);

            var promoCodeData = await _promoCodeCouponsService.GetAsync(command.CouponCode);
            var promoCode = PromoCodeDomain.Create(promoCodeData, DateTime.UtcNow);

            if (!promoCode.IsCompatible(price))
                throw new AppException(HttpStatusCode.NotFound, $"Coupon code '{command.CouponCode}' is not compatible with {command.PlanName}");

            return PaymentPriceDomain.Create(price, promoCodeData, DateTime.UtcNow);
        }
    }
}