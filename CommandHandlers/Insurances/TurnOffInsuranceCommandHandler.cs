using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Insurances;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Application.Commands.Subscriptions;
using MediatR;
using WildHealth.Application.Events.Insurances;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;
using WildHealth.Infrastructure.Data.Queries;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.CommandHandlers.Payments.Flows;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.PromoCodes;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class TurnOffInsuranceCommandHandler : IRequestHandler<TurnOffInsuranceCommand>
{
    private readonly IPatientsService _patientsService;
    private readonly IPaymentPlansService _paymentPlansService;
    private readonly IGeneralRepository<PaymentPlan> _paymentPlansRepository;
    private readonly IGeneralRepository<PromoCodeCoupon> _promoCodeCouponsRepository;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public TurnOffInsuranceCommandHandler(
        IPatientsService patientsService, 
        IPaymentPlansService paymentPlansService, 
        IGeneralRepository<PaymentPlan> paymentPlansRepository,
        IGeneralRepository<PromoCodeCoupon> promoCodeCouponsRepository,
        IMediator mediator,
        ILogger<TurnOffInsuranceCommandHandler> logger)
    {
        _patientsService = patientsService;
        _paymentPlansService = paymentPlansService;
        _paymentPlansRepository = paymentPlansRepository;
        _promoCodeCouponsRepository = promoCodeCouponsRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(TurnOffInsuranceCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Insurance turn off process for patient with [Id] = {command.PatientId} started.");
        
        var patientSpecification = PatientSpecifications.PatientWithSubscription;

        var patient = await _patientsService.GetByIdAsync(command.PatientId, patientSpecification);

        var subscription = patient.MostRecentSubscription;

        if (!AssertSubscriptionType(subscription, SubscriptionType.Insurance))
        {
            _logger.LogInformation($"Insurance turn off process for patient with [Id] = {command.PatientId} was skipped.");
            
            return;
        }

        var currentPlan = await GetCurrentPlan(subscription);

        var currentPaymentPrice = subscription.PaymentPrice;

        var currentPaymentCoupon = subscription.PaymentPrice.PaymentCoupon;
        
        var activePlans = await _paymentPlansService.GetActiveAsync(currentPlan.PracticeId, subscription.EmployerProduct);

        var activePromoCodeCoupons = await _promoCodeCouponsRepository.All().ToArrayAsync(cancellationToken);
        
        var currentPromoCodeCouponCode = subscription.PromoCodeCoupon?.Code;

        var currentPaymentStrategy = subscription.PaymentPrice.Strategy;

        var flow = new GetAlternativePaymentPriceFlow(
            currentPlan: currentPlan, 
            currentPaymentPrice: currentPaymentPrice,
            currentPaymentStrategy: currentPaymentStrategy, 
            currentPaymentCoupon: currentPaymentCoupon,
            currentPromoCodeCouponCode: currentPromoCodeCouponCode,
            activePlans: activePlans.ToArray(),
            activePromoCodeCoupons: activePromoCodeCoupons.ToArray());

        var (altPaymentPrice, couponCode) = flow.Execute();

        var changeSubscriptionCommand = new ChangeSubscriptionPaymentPriceCommand(
            currentSubscriptionId: subscription.GetId(),
            newPaymentPriceId: altPaymentPrice.GetId(),
            startDate: null,
            endDate: null,
            couponCode: couponCode,
            employerProductId: null
        );

        var newSubscription = await _mediator.Send(changeSubscriptionCommand, cancellationToken);
        await _mediator.Publish(new PatientUpdatedEvent(patient.GetId(), Enumerable.Empty<int>()), cancellationToken);

        await _mediator.Publish(new SubscriptionPaymentFlowChangedEvent(
            subscriptionId: subscription.GetId(),
            priorFlow: subscription.GetSubscriptionType().ToString(),
            newFlow: newSubscription.GetSubscriptionType().ToString()
        ));

        _logger.LogInformation($"Insurance turn off process for patient with [Id] = {command.PatientId} completed.");
    }
    
    #region private

    private async Task<PromoCodeCoupon[]> AllPromoCodes()
    {
        return await _promoCodeCouponsRepository.All().ToArrayAsync();
    }

    private async Task<string?> GetEquivalentPromoCodeCoupon(PaymentCoupon pc)
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

        var promoCodeCoupon = await _promoCodeCouponsRepository
            .All()
            .Where(o => o.ExpirationDate == null || o.ExpirationDate > DateTime.UtcNow)
            .Where(o => o.DiscountType == DiscountType.Percentage)
            .Where(o => o.Discount == Convert.ToInt32(discountAmount))
            .FirstOrDefaultAsync();

        return promoCodeCoupon?.Code;
    }

    private async Task<PaymentPlan> GetCurrentPlan(Subscription subscription)
    {
        var currentPaymentPrice = subscription.PaymentPrice;
        var paymentPeriod = currentPaymentPrice.PaymentPeriod;
        var paymentPlanId = paymentPeriod.PaymentPlanId;
        var practiceId = paymentPeriod.PaymentPlan.PracticeId;
        
        var paymentPlan = await _paymentPlansService.GetPlanAsync(
            paymentPlanId: paymentPlanId,
            paymentPeriodId: paymentPeriod.GetId(),
            practiceId: practiceId
        );

        return paymentPlan;
    }
    
    /// <summary>
    /// Asserts subscription is insurance type
    /// </summary>
    /// <param name="subscription"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private bool AssertSubscriptionType(Subscription subscription, SubscriptionType type)
    {
        if (subscription is null)
        {
            return false;
        }

        return subscription.GetSubscriptionType() == type;
    }

    #endregion
}