using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Services.Agreements;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Events.Subscriptions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.PromoCodes;
using WildHealth.Application.Utils.DateTimes;
using MediatR;
using WildHealth.Application.Services.Users;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.PromoCodes;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Subscriptions;
using WildHealth.IntegrationEvents.Subscriptions.Payloads;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Subscriptions
{
    /// <summary>
    /// Provides buy new subscription command handler
    /// </summary>
    public class BuyNewSubscriptionCommandHandler : IRequestHandler<BuyNewSubscriptionCommand, Subscription>
    {
        private readonly IIntegrationServiceFactory _integrationServiceFactory;
        private readonly ILogger<BuyNewSubscriptionCommandHandler> _logger;
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly IAgreementsService _agreementsService;
        private readonly IPaymentService _paymentService;
        private readonly IMediator _mediator;
        private readonly IPromoCodeCouponsService _promoCodeService;
        private readonly IDateTimeProvider _dateTime;
        private readonly MaterializeFlow _materialize;
        private readonly IEventBus _eventBus;
        private readonly IUsersService _usersService;
        
        public BuyNewSubscriptionCommandHandler(
            IIntegrationServiceFactory integrationServiceFactory,
            ILogger<BuyNewSubscriptionCommandHandler> logger,
            IPaymentPlansService paymentPlansService,
            IAgreementsService agreementsService,
            IPaymentService paymentService,
            IMediator mediator, 
            IPromoCodeCouponsService promoCodeService, 
            IDateTimeProvider dateTime, 
            MaterializeFlow materialize, 
            IEventBus eventBus, 
            IUsersService usersService)
        {
            _integrationServiceFactory = integrationServiceFactory;
            _paymentPlansService = paymentPlansService;
            _agreementsService = agreementsService;
            _paymentService = paymentService;
            _mediator = mediator;
            _promoCodeService = promoCodeService;
            _dateTime = dateTime;
            _materialize = materialize;
            _eventBus = eventBus;
            _usersService = usersService;
            _logger = logger;
        }
        
        /// <summary>
        /// Handles buy new subscription command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Subscription> Handle(BuyNewSubscriptionCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Buying new subscription for patient with [Id] {command.Patient.Id} has been started");

            var integrationId = string.Empty;
            
            var employerProduct = command.EmployerProduct;
            
            var paymentPrice = await _paymentPlansService.GetPaymentPriceByIdAsync(command.PaymentPriceId);

            var integrationVendor = await _paymentService.GetIntegrationVendorAsync(command.Patient.User.PracticeId);

            Subscription? subscription = null;
            try
            {
                var coupon = await _promoCodeService.GetAsync(command.PromoCode, command.Patient.User.PracticeId);
                var isFirstPurchase = !command.NoStartupFee; 
                
                subscription = await CreateSubscription(command, paymentPrice, employerProduct, coupon, isFirstPurchase);

                _logger.LogInformation($"Subscription for patient with [Id] {command.Patient.Id} is created");

                if (command.ConfirmAgreements)
                {
                    await _agreementsService.ConfirmAgreementsAsync(
                        patient: command.Patient,
                        subscription: subscription!,
                        models: command.Agreements
                    );
                }

                _logger.LogInformation($"Agreements for patient with [Id] {command.Patient.Id} are confirmed");

                _logger.LogInformation($"Payment subscription for patient with [Id] {command.Patient.Id} has been started");

                var originSubscription = await _paymentService.BuySubscriptionAsyncV2(command.Patient, subscription!, paymentPrice, employerProduct, coupon, isFirstPurchase);
                
                integrationId = originSubscription.Id;

                await _paymentService.ProcessSubscriptionPaymentAsync(command.Patient, integrationId);

                await new MarkSubscriptionAsPaidFlow(subscription!, integrationId, integrationVendor).Materialize(_materialize);
                
                await _mediator.Publish(new SubscriptionCreatedEvent(command.Patient), cancellationToken);

                _logger.LogInformation($"Payment subscription for patient with [Id] {command.Patient.Id} successfully finished");
            }
            catch (Exception ex)
            {
                await ProcessPaymentException(subscription, integrationId, command.Patient.User.PracticeId, ex);

                throw;
            }

            return subscription!;
        }

        private async Task<Subscription> CreateSubscription(BuyNewSubscriptionCommand command, PaymentPrice paymentPrice,EmployerProduct employerProduct, PromoCodeCoupon? coupon, bool isFirstPurchase)
        {
            try
            {
                return await new CreateSubscriptionFlow(command.Patient, paymentPrice, employerProduct, coupon, isFirstPurchase, _dateTime.UtcNow())
                    .Materialize(_materialize)
                    .Select<Subscription>();
            }
            catch (Exception ex) when (ex is AppException or ValidationException && coupon is not null)
            {
                var payload = new SignupFailedBecauseOfInactivePromoCodePayload(coupon.Code, ex.Message);
                var universalId = await _usersService.GetUserUniversalId(command.Patient.GetId());
                await _eventBus.Publish(new SubscriptionIntegrationEvent(payload,
                    new PatientMetadataModel(command.Patient.UserId, universalId.ToString()),
                    _dateTime.UtcNow()));
                throw;
            }
        }

        private async Task ProcessPaymentException(Subscription? subscription, string integrationId, int practiceId, Exception ex)
        {
            _logger.LogError($"Buying new subscription for patient with [Id] {subscription?.PatientId} is failed. {ex}");

            if (!string.IsNullOrEmpty(integrationId))
            {
                var integrationService = await _integrationServiceFactory.CreateAsync(practiceId);
                    
                await integrationService.TryCancelSubscriptionAsync(subscription, DateTime.UtcNow, "Process was failed");
            }
        }
    }
}