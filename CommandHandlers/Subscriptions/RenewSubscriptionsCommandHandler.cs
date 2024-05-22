using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Commands.Products;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Events.Payments;
using WildHealth.Application.Services.Agreements;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Common.Models.Subscriptions;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Shared.Data.Managers.TransactionManager;
using MediatR;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Application.Services.PaymentPrices;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Application.Services.PromoCodes;

namespace WildHealth.Application.CommandHandlers.Subscriptions;

public class RenewSubscriptionsCommandHandler : IRequestHandler<RenewSubscriptionsCommand>
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPatientsService _patientsService;
    private readonly IIntegrationServiceFactory _integrationServiceFactory;
    private readonly IAgreementsService _agreementsService;
    private readonly IPracticeService _practicesService;
    private readonly ITransactionManager _transactionManager;
    private readonly IMediator _mediator;
    private readonly ILogger<RenewSubscriptionsCommandHandler> _logger;
    private readonly IPromoCodeCouponsService _promoCodeService;
    private readonly IPaymentPriceService _paymentPriceService;
    private readonly MaterializeFlow _materializer;
    private readonly IPaymentPlansService _paymentPlansService;
    private readonly IPaymentService _paymentService;
    private readonly IPatientProfileService _patientProfileService;
    
    public RenewSubscriptionsCommandHandler(
        ISubscriptionService subscriptionService,
        IPatientsService patientsService,
        IIntegrationServiceFactory integrationServiceFactory,
        IAgreementsService agreementsService,
        IPracticeService practicesService,
        ITransactionManager transactionManager,
        IMediator mediator,
        ILogger<RenewSubscriptionsCommandHandler> logger, 
        IPromoCodeCouponsService promoCodeService, 
        IPaymentPriceService paymentPriceService, 
        MaterializeFlow materializer, 
        IPaymentPlansService paymentPlansService, 
        IPaymentService paymentService, 
        IPatientProfileService patientProfileService)
    {
        _subscriptionService = subscriptionService;
        _patientsService = patientsService;
        _integrationServiceFactory = integrationServiceFactory;
        _agreementsService = agreementsService;
        _practicesService = practicesService;
        _transactionManager = transactionManager;
        _mediator = mediator;
        _logger = logger;
        _promoCodeService = promoCodeService;
        _paymentPriceService = paymentPriceService;
        _materializer = materializer;
        _paymentPlansService = paymentPlansService;
        _paymentService = paymentService;
        _patientProfileService = patientProfileService;
    }

    public async Task Handle(RenewSubscriptionsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Renewing subscriptions has been started for the date: {request.Date}");

        var practices = await _practicesService.GetActiveAsync();
            
        foreach (var practice in practices)
        {
            var from = request.Date.Date;
            var to = from.AddDays(1).AddHours(8);
            
            var finishingSubscriptions = await _subscriptionService.GetFinishingSubscriptionsAsync(
                from: from, 
                to: to, 
                practiceId: practice.GetId()
            );;

            _logger.LogInformation($"{finishingSubscriptions.Count()} subscriptions for practice {practice.Name} are found to renew");

            var report = new List<RenewSubscriptionReportModel>();
                
            foreach (var subscription in finishingSubscriptions)
            {
                if (!CheckIfLocalDateMatchWithToday(subscription, from))
                {
                    _logger.LogInformation("Subscription {subscriptionId} was skipped because local start time doesnt match with requested date", subscription.GetId());
                    
                    continue;
                }
                
                var sw = Stopwatch.StartNew();
                report.Add(await RenewSubscriptionProcess(subscription, request.Date, cancellationToken));
                _logger.LogInformation($"{nameof(RenewSubscriptionProcess)} is done for subscription {subscription.Id}. Timespent: {sw.Elapsed}");
            }

            await _mediator.Publish(new RenewalSubscriptionFinishEvent(report, practice.GetId()), cancellationToken);
        }
    }

    private bool CheckIfLocalDateMatchWithToday(Subscription subscription, DateTime utcNow)
    {
        var timeZoneId = subscription.Patient.TimeZone;
        
        var utcEndDate = subscription.EndDate;

        if (string.IsNullOrEmpty(timeZoneId))
        {
            return utcEndDate.Date == utcNow.Date;
        }
        
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        
        var localEndTime = TimeZoneInfo.ConvertTimeFromUtc(utcEndDate, timeZone);

        return localEndTime.Date == utcNow.Date;
    }
    
    private async Task<RenewSubscriptionReportModel> RenewSubscriptionProcess(Subscription currentSubscription, DateTime now, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Renew subscription with [Id] = {currentSubscription.Id} started");

        var patient = await _patientsService.GetByIdAsync(currentSubscription.PatientId);
            
        string result;
        Subscription? newSubscription = null;
        await using var transaction = _transactionManager.BeginTransaction();
      
        try
        {
            var cancellationRequest = currentSubscription.CancellationRequest;
            var paymentInfo = await GetCouponCodeAndPaymentPrice(currentSubscription, now);
            var paymentPrice = await _paymentPlansService.GetPaymentPriceByIdAsync(paymentInfo.PaymentPriceId);
            var coupon = await _promoCodeService.GetAsync(paymentInfo.Code, currentSubscription.PracticeId!.Value);
            var integrationVendor = await _paymentService.GetIntegrationVendorAsync(currentSubscription.PracticeId!.Value);
            var integrationService = await _integrationServiceFactory.CreateAsync(currentSubscription.Patient.User.PracticeId);
            var integrationSubscription = await integrationService.GetPatientSubscriptionAsync(patient, currentSubscription);
            var patientProfileLink = await _patientProfileService.GetProfileLink(currentSubscription.Patient.GetId(), currentSubscription.PracticeId!.Value);
            
            newSubscription = (await new RenewSubscriptionFlow(
                currentSubscription,
                integrationSubscription,
                currentSubscription.Patient,
                paymentPrice,
                currentSubscription.EmployerProduct,
                coupon,
                integrationVendor, 
                now,
                PatientProfileLink: patientProfileLink).Materialize(_materializer)).Select<Subscription, EntityAction.Add>();
            
            await _mediator.Send(new ExpirePatientProductsCommand(patient.GetId(), "Renew subscription"), cancellationToken);
            
            var createBuildInProductsCommand = new CreateBuiltInProductsCommand(newSubscription!.GetId());
            await _mediator.Send(createBuildInProductsCommand, cancellationToken);
                
            if (cancellationRequest is not null)
            {
                await _subscriptionService.ScheduleCancellationAsync(
                    subscription: newSubscription,
                    cancellationType: cancellationRequest.ReasonType,
                    cancellationReason: cancellationRequest.Reason,
                    cancellationDate: cancellationRequest.Date
                );
            }

            await _agreementsService.CopyAgreementsAsync(patient, currentSubscription, newSubscription);

            await transaction.CommitAsync(cancellationToken);

            result = "Success";
                
            _logger.LogInformation($"Renew subscription for patient with [Id] = {patient.Id} completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Renewing subscription for patient with [id] = {patient.Id} was failed. {ex}");
            await transaction.RollbackAsync(cancellationToken);
            result = ex.Message;
        }
            
        return new RenewSubscriptionReportModel
        {
            Email = patient.User.Email,
            Period = newSubscription?.PaymentPrice.PaymentPeriod.PeriodInMonths.ToString(),
            Result = result,
            FirstName = patient.User.FirstName,
            LastName = patient.User.LastName,
            PaymentPlan = newSubscription?.PaymentPrice.PaymentPeriod.PaymentPlan.DisplayName,
            Price = newSubscription?.Price ?? 0
        };
    }

    private async Task<ResolvePromoCodeFlowResult> GetCouponCodeAndPaymentPrice(Subscription currentSubscription, DateTime now)
    {
        var paymentPrices =
            await _paymentPriceService.GetByPeriodIdAsync(currentSubscription.PaymentPrice.PaymentPeriodId);
            
        // Only new promo codes are directly linked with subscriptions
        var subscriptionPromoCode = currentSubscription.PromoCodeCouponId.HasValue
            ? await _promoCodeService.GetByIdAsync(currentSubscription.PromoCodeCouponId.Value)
            : null;
            
        // When old promo code discount is used then find the equivalent new promo code replacement for it 
        var oldPromoCodeReplacement = await _promoCodeService.GetAsync(
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