using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.PromoCodes;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Application.Services.PromoCodes;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Models.Payments;
using WildHealth.Domain.Models.Payment;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Subscriptions;
using WildHealth.IntegrationEvents.Subscriptions.Payloads;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.PromoCodes;

public class ApplyPromoCodeCommandHandler : IRequestHandler<ApplyPromoCodeCommand, List<PaymentPriceModel>>
{
    private readonly IPaymentPlansService _paymentPlansService;
    private readonly IPromoCodeCouponsService _couponsService;
    private readonly IEventBus _eventBus;
    private readonly IUsersService _usersService;
    private readonly IDateTimeProvider _dateTime;
    
    public ApplyPromoCodeCommandHandler(
        IPaymentPlansService paymentPlansService, 
        IPromoCodeCouponsService couponsService, 
        IEventBus eventBus, 
        IUsersService usersService, 
        IDateTimeProvider dateTime)
    {
        _paymentPlansService = paymentPlansService;
        _couponsService = couponsService;
        _eventBus = eventBus;
        _usersService = usersService;
        _dateTime = dateTime;
    }

    public async Task<List<PaymentPriceModel>> Handle(ApplyPromoCodeCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return await HandleCore(command);
        }
        catch (Exception e)
        {
            var payload = new SignupFailedBecauseOfInactivePromoCodePayload(command.Code, e.Message);
            
            var user = await _usersService.GetByIdAsync(command.UserId);
            if (user is not null)
                await _eventBus.Publish(new SubscriptionIntegrationEvent(payload, new PatientMetadataModel(user.GetId(), user.UniversalId.ToString()), _dateTime.UtcNow()), cancellationToken);
            
            throw;
        }
    }

    private async Task<List<PaymentPriceModel>> HandleCore(ApplyPromoCodeCommand command)
    {
        var paymentPlanData = await _paymentPlansService.GetByPaymentPeriodId(command.PaymentPeriodId);
        var couponData = await _couponsService.GetAsync(command.Code.ToUpper(), paymentPlanData?.PracticeId ?? 0)
                         ?? throw new AppException(HttpStatusCode.NotFound, $"Coupon: {command.Code} does not exist");

        var couponDomain = PromoCodeDomain.Create(couponData, DateTime.UtcNow);

        couponDomain.ThrowIfNotUsable(command.PaymentPriceType);

        var paymentPlanDomain = PaymentPlanPricingDomain.Create(paymentPlanData, couponDomain);

        var priceOptions = paymentPlanDomain.PriceOptions(command.PaymentPriceType);

        return priceOptions;
    }
}