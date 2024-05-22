using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Services.Founders;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.CommandHandlers.Subscriptions.Base;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.PaymentIssues;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Application.Services.PromoCodes;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Integration.Models.Payments;
using WildHealth.Integration.Models.Subscriptions;
using WildHealth.Application.Events.Payments;
using OneOf.Monads;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Subscriptions
{
    /// <summary>
    /// Provides replace subscription command
    /// </summary>
    public class ReplaceSubscriptionCommandHandler : BaseCancelSubscriptionsCommandHandler, IRequestHandler<ReplaceSubscriptionCommand, Subscription>
    {
        private readonly IPaymentService _paymentService;
        private readonly IPatientsService _patientsService;
        private readonly IFoundersService _foundersService;
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly IPromoCodeCouponsService _promoCodeService;
        private readonly ITransactionManager _transactionManager;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<ReplaceSubscriptionCommandHandler> _logger;

        public ReplaceSubscriptionCommandHandler(
            IMediator mediator,
            IPaymentService paymentService,
            IIntegrationServiceFactory integrationServiceFactory,
            IPatientsService patientsService,
            IFoundersService foundersService,
            IPaymentPlansService paymentPlansService,
            IPromoCodeCouponsService promoCodeService,
            ISubscriptionService subscriptionService,
            ITransactionManager transactionManager,
            IDateTimeProvider dateTimeProvider,
            ILogger<ReplaceSubscriptionCommandHandler> logger, 
            MaterializeFlow materializeFlow, 
            IPaymentIssuesService paymentIssuesService) : base(subscriptionService, integrationServiceFactory, mediator, materializeFlow, paymentIssuesService)
        {
            _paymentService = paymentService;
            _patientsService = patientsService;
            _foundersService = foundersService;
            _paymentPlansService = paymentPlansService;
            _promoCodeService = promoCodeService;
            _transactionManager = transactionManager;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }

        /// <summary>
        /// Handles command and replace trial subscription
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Subscription> Handle(ReplaceSubscriptionCommand command, CancellationToken cancellationToken)
        {
            var utcNow = _dateTimeProvider.UtcNow();
            
            var isFounderPlan = await _paymentPlansService.IsFounderPlanAsync(
                paymentPeriodId: command.PaymentPeriodId,
                paymentPriceId: command.PaymentPriceId);

            var patient = await _patientsService.GetByIdAsync(command.PatientId);

            var founder = await GetFounderAsync(command.FounderId);
            
            var paymentPrice = await _paymentPlansService.GetPaymentPriceByIdAsync(command.PaymentPriceId);

            var coupon = await _promoCodeService.GetAsync(command.PromoCode, patient.User.PracticeId);
            
            var patientPaymentIssues = await PaymentIssuesService.GetActiveAsync(patient.GetId());
            
            var recentSubscription = await SubscriptionService.GetAsync(patient.MostRecentSubscription.GetId());

            var employerProduct = recentSubscription.EmployerProduct;
            
            var flow = new ReplaceSubscriptionFlow(
                Patient: patient,
                NewPaymentPrice: paymentPrice,
                EmployerProduct: employerProduct,
                Coupon: coupon,
                RecentSubscription: recentSubscription,
                PaymentIssues: patientPaymentIssues,
                Founder: founder,
                IsFounderPlan: isFounderPlan,
                StartDate: utcNow,
                UtcNow: utcNow
            );

            var integrationService = await IntegrationServiceFactory.CreateAsync(patient.User.PracticeId);
            
            await using var transaction = _transactionManager.BeginTransaction();

            Subscription? newSubscription = null;

            var integrationId = string.Empty;
            
            try
            {
                if (founder is not null)
                {
                    await AssignFounderToPatientAsync(patient, founder);
                }
                
                // Do not want to publish a cancel event
                await CancelSubscriptionAsync(
                    subscription: recentSubscription, 
                    type: CancellationReasonType.Replaced,
                    reason: "Patient bought new subscription"
                );

                newSubscription = await flow
                    .Materialize(MaterializeFlow)
                    .Select<Subscription>();
                
                var originSubscription = await _paymentService.BuySubscriptionAsyncV2(patient, newSubscription!, paymentPrice, employerProduct, coupon, false, recentSubscription);
                
                integrationId = originSubscription.Id;
                
                await _paymentService.ProcessSubscriptionPaymentAsync(patient, originSubscription.Id);

                var paymentResult = await _paymentService.ProcessSubscriptionPaymentAsync(patient, originSubscription.Id).ToTry();

                AssertPaymentSuccess(paymentResult, IsFirstBilling(recentSubscription, originSubscription, integrationService.IntegrationVendor));
                
                await new MarkSubscriptionAsPaidFlow(newSubscription, integrationId, integrationService.IntegrationVendor).Materialize(MaterializeFlow);

                await Mediator.Send(new CreateBuiltInProductsCommand(newSubscription.GetId()), cancellationToken);
                
                var buyAddOnsCommand = new BuyAddOnsCommand(
                    patient: patient,
                    selectedAddOnIds: command.AddOnIds,
                    paymentPriceId: command.PaymentPriceId,
                    buyRequiredAddOns: IsRequiredAddOns(recentSubscription),
                    practiceId: patient.User.PracticeId,
                    skipPaymentError: true,
                    employerProduct: recentSubscription.EmployerProduct
                );

                await Mediator.Send(buyAddOnsCommand, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                
                await ProcessPaymentException(newSubscription, integrationId, patient.User.PracticeId, ex);
                
                throw;
            }
            finally
            {
                // IMPORTANT: patient should be unlocked after transaction
                await _patientsService.UnlockPatientAsync(patient);
            }
            
            await PublishSubscriptionChangedEventAsync(patient, newSubscription, recentSubscription, command.AddOnIds);
            
            return newSubscription;
        }
        
        #region private

        private async Task<Founder?> GetFounderAsync(int? founderId)
        {
            return founderId.HasValue
                ? await _foundersService.GetByIdAsync(founderId.Value)
                : null;
        }
        
        private async Task AssignFounderToPatientAsync(Patient patient, Founder founder)
        {
            var employeeIds = patient.
                GetAssignedEmployeesIds()
                .Concat(new [] {founder.EmployeeId})
                .Distinct()
                .ToArray();

            await _patientsService.AssignToEmployeesAsync(patient, employeeIds);
        }
        
        private async Task ProcessPaymentException(Subscription? subscription, string integrationId, int practiceId, Exception ex)
        {
            _logger.LogError($"Replacing subscription for patient with [Id] {subscription?.PatientId} is failed. {ex}");

            if (!string.IsNullOrEmpty(integrationId))
            {
                var integrationService = await IntegrationServiceFactory.CreateAsync(practiceId);
                    
                await integrationService.TryCancelSubscriptionAsync(subscription, _dateTimeProvider.UtcNow(), "Process was failed");
            }
        }
        
        private void AssertPaymentSuccess(Try<PaymentIntegrationModel?> paymentResult, bool isFirstBilling)
        {
            // If payment result is null - it means subscription was paid automatically on the creation state
            // And we don't have any payment issues int this case
            // Also, if it's first payment and result failed - we want to raise exception and rollback all changes
            // Because Stripe doesn't provide billing retry mechanism for subscriptions without any payments.
            if (paymentResult.IsError() && isFirstBilling)
            {
                throw paymentResult.IsError()
                    ? paymentResult.Exception()
                    : new DomainException("Error processing subscription payment");
            }
        }
        
        private bool IsRequiredAddOns(Subscription? subscription)
        {
            if (subscription is null)
            {
                return false;
            }

            return subscription.IsTrial() || subscription.PaymentPrice.IsNotIntegratedPlan();
        }

        private bool IsFirstBilling(
            Subscription recentSubscription, 
            SubscriptionIntegrationModel newSubscription, 
            IntegrationVendor integrationVendor)
        {
            return !recentSubscription.IsLinkedWithIntegrationSystem()
                   || recentSubscription.GetIntegrationId(integrationVendor) != newSubscription.Id;
        }
        
        private async Task PublishSubscriptionChangedEventAsync(Patient patient, Subscription newSubscription, Subscription recentSubscription, int[] addOnIds)
        {
            var @event = new SubscriptionChangedEvent(
                patient: patient,
                newSubscription: newSubscription,
                previousSubscription: recentSubscription,
                patientAddOnIds: addOnIds
            );
            
            await Mediator.Publish(@event);
        }

        #endregion
    }
}