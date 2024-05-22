using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Integration.Events;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Integration.Models.Subscriptions;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Enums.Integrations;
using MediatR;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.PaymentIssues;

namespace WildHealth.Application.EventHandlers.Integration
{
    public class IntegrationSubscriptionUpdatedEventHandler : INotificationHandler<IntegrationSubscriptionUpdatedEvent>
    {
        private const CancellationReasonType CancellationType = CancellationReasonType.CanceledInPaymentSystem;
        private const string CancellationReason = "Subscription was canceled in integration system";

        private readonly string[] _cancelledStatus = { "ended", "canceled" };
        
        private readonly IPatientsService _patientsService;
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ITransactionManager _transactionManager;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly MaterializeFlow _materializeFlow;
        private readonly IPaymentIssuesService _paymentIssuesService;

        public IntegrationSubscriptionUpdatedEventHandler(
            IPatientsService patientsService,
            IPaymentPlansService paymentPlansService,
            ISubscriptionService subscriptionService,
            ITransactionManager transactionManager,
            ILogger<IntegrationSubscriptionUpdatedEventHandler> logger,
            IMediator mediator, 
            MaterializeFlow materializeFlow, 
            IPaymentIssuesService paymentIssuesService)
        {
            _patientsService = patientsService;
            _paymentPlansService = paymentPlansService;
            _subscriptionService = subscriptionService;
            _transactionManager = transactionManager;
            _logger = logger;
            _mediator = mediator;
            _materializeFlow = materializeFlow;
            _paymentIssuesService = paymentIssuesService;
        }

        public async Task Handle(IntegrationSubscriptionUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var originSubscription = notification.Subscription;

            _logger.LogInformation($"Update membership from integration system for patient with [IntegrationId]: {originSubscription.PatientId} was started.");

            var patient = await GetPatientAsync(originSubscription.PatientId, notification.Vendor);

            if (patient is null)
            {
                _logger.LogWarning($"Patient with [IntegrationId]: {originSubscription.PatientId} does not exist.");
                
                return;
            }

            if (patient.IsLockedToUpdates)
            {
                _logger.LogWarning($"Patient with [IntegrationId]: {originSubscription.PatientId} is locked for updates.");
                
                return;
            }

            var existingSubscription = GetExistingSubscription(patient, originSubscription);

            if (existingSubscription is null)
            {
                _logger.LogWarning($"Subscription for patient with [IntegrationId]: {originSubscription.PatientId} with [IntegrationId]: {originSubscription.Id} does not exist.");
                
                return;
            }
            
            var subscription = await _subscriptionService.GetAsync(existingSubscription.GetId());

            var shouldCancel = ShouldCancel(originSubscription, subscription);
            
            //////////////////////////////////////////////////////////////////////////////////////////
            /// Removing these 2 options because we no longer want to manage subscriptions through
            /// Stripe.  Any modification of subscriptions should be managed in clarity
            /// and that information flow to stripe
            //////////////////////////////////////////////////////////////////////////////////////////
            
            // if (!shouldCancel && ShouldChangePaymentStrategy(originSubscription, subscription))
            // {
            //     await ChangeSubscriptionPaymentStrategyAsync(originSubscription, subscription);
            // }
            //
            // if (!shouldCancel && ShouldChangePaymentPlan(originSubscription, subscription, notification.Vendor))
            // {
            //     await ChangeSubscriptionPaymentPlanAsync(originSubscription, subscription, notification.Vendor);
            // }
            
            if (shouldCancel)
            {
                await CancelSubscriptionAsync(subscription, patient);
            }

            if (ShouldScheduleCancellation(originSubscription))
            {
                await ScheduleCancellationAsync(originSubscription, subscription);
            }

            _logger.LogInformation($"Update membership from integration system for patient with [IntegrationId]: {originSubscription.PatientId} was successfully ended.");
        }

        #region private 

        /// <summary>
        /// Returns if should change subscription payment strategy
        /// </summary>
        /// <param name="originSubscription"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        private bool ShouldChangePaymentStrategy(SubscriptionIntegrationModel originSubscription, Subscription subscription)
        {
            var period = subscription.PaymentStrategy switch
            {
                PaymentStrategy.FullPayment => 12,
                PaymentStrategy.PartialPayment => 1,
                _ => throw new ArgumentException("Unsupported payment strategy")
            };

            // Payment strategy should be changed in case if origin period doesn't match with subscription period
            return originSubscription.PeriodInMonths.HasValue && originSubscription.PeriodInMonths != period;
        }
        
        /// <summary>
        /// Changes subscription payment strategy
        /// </summary>
        /// <param name="originSubscription"></param>
        /// <param name="currentSubscription"></param>
        /// <returns></returns>
        private async Task ChangeSubscriptionPaymentStrategyAsync(SubscriptionIntegrationModel originSubscription, Subscription currentSubscription)
        {
            _logger.LogInformation($"Change membership from integration system is started.");

            var paymentStrategy = GetOriginSubscriptionPaymentStrategy(originSubscription);

            currentSubscription.ChangePaymentStrategy(paymentStrategy);
            
            await _transactionManager.Run(async () =>
            {
                await _subscriptionService.UpdateSubscriptionAsync(currentSubscription);
            }, ex => _logger.LogError($"Update membership from integration system was failed. {ex}"));
            
            _logger.LogInformation($"Change membership from integration system is successful.");
        }

        /// <summary>
        /// Returns if should change subscription payment plan
        /// </summary>
        /// <param name="originSubscription"></param>
        /// <param name="subscription"></param>
        /// <param name="vendor"></param>
        /// <returns></returns>
        private bool ShouldChangePaymentPlan(SubscriptionIntegrationModel originSubscription, Subscription subscription, IntegrationVendor vendor)
        {
            // Payment plan should be changed in case if current subscription
            // * has different plan than origin subscription
            return subscription.PaymentPrice.GetIntegrationId(vendor) != originSubscription.PlanId;
        }
        
        /// <summary>
        /// Changes subscription payment plan
        /// </summary>
        /// <param name="originSubscription"></param>
        /// <param name="currentSubscription"></param>
        /// <param name="vendor"></param>
        /// <returns></returns>
        private async Task ChangeSubscriptionPaymentPlanAsync(
            SubscriptionIntegrationModel originSubscription, 
            Subscription currentSubscription,
            IntegrationVendor vendor)
        {
            _logger.LogInformation($"Change subscription payment plan from integration system is started.");
            
            var paymentPrice = await GetCorrespondingPaymentPriceAsync(originSubscription, vendor);

            if (paymentPrice is null)
            {
                _logger.LogError($"Payment price with [IntegrationId]: {originSubscription.ProductIntegrationId}, [Amount] = {originSubscription.RateInCents}, [Interval] = {originSubscription.Interval} does not exist.");
                    
                return;
            }

            await _mediator.Send(new ChangeSubscriptionPaymentPriceCommand(
                startDate: null,
                endDate: null,
                currentSubscriptionId: currentSubscription.GetId(),
                newPaymentPriceId: paymentPrice.GetId(),
                couponCode: null,
                employerProductId: null
            ));

            _logger.LogInformation($"Change membership from integration system is successful.");
        }

        /// <summary>
        /// Returns if should cancel subscription
        /// </summary>
        /// <param name="originSubscription"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        private bool ShouldCancel(SubscriptionIntegrationModel originSubscription, Subscription subscription)
        {
            // To cancel subscription origin subscription must have corresponding status
            return _cancelledStatus.Contains(originSubscription.Status);
        }
        
        private async Task CancelSubscriptionAsync(Subscription currentSubscription, Patient patient)
        {
            _logger.LogInformation($"Cancel membership from integration system is started.");

            // If it's already canceled, just return
            if (currentSubscription.IsCanceled())
            {
                return;
            }

            var patientPaymentIssues = await _paymentIssuesService.GetActiveAsync(currentSubscription.PatientId);
            await _transactionManager.Run(async () =>
            {
                await new CancelSubscriptionFlow(
                    currentSubscription,
                    CancellationType,
                    CancellationReason,
                    DateTime.UtcNow,
                    patientPaymentIssues).Materialize(_materializeFlow);
            }, ex => _logger.LogError($"Cancel membership from integration system was failed. {ex}"));

            _logger.LogInformation($"Change membership from integration system is successful.");
        }

        /// <summary>
        /// Returns if should schedule cancellation
        /// </summary>
        /// <param name="originSubscription"></param>
        /// <returns></returns>
        private bool ShouldScheduleCancellation(SubscriptionIntegrationModel originSubscription)
        {
            // To schedule cancellation origin subscription must have specified end date
            return originSubscription.EndDate.HasValue;
        }
        
        private async Task ScheduleCancellationAsync(SubscriptionIntegrationModel originSubscription, Subscription subscription)
        {
            _logger.LogInformation($"Schedule subscription cancellation from integration system has been started.");

            var cancellationDate = originSubscription.EndDate ?? DateTime.UtcNow;
            
            await _transactionManager.Run(async () =>
            {
                await _subscriptionService.ScheduleCancellationAsync(
                    subscription: subscription, 
                    cancellationType: CancellationReasonType.CanceledInPaymentSystem,
                    cancellationReason: CancellationReason,
                    cancellationDate);
            }, ex =>
            {
                _logger.LogError($"Schedule subscription cancellation from integration system has been failed. {ex}");
            });
            
            _logger.LogInformation($"Schedule subscription cancellation from integration system has been successfully finished.");
        }

        /// <summary>
        /// Returns existing subscription witch match 
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="originSubscription"></param>
        /// <returns></returns>
        private Subscription? GetExistingSubscription(Patient patient, SubscriptionIntegrationModel originSubscription)
        {
            // Possible to hve more than one subscription with same integration id.
            // Please always take the latest one, using ordering by id.
            return patient
                .Subscriptions
                .OrderBy(x => x.Id)
                .LastOrDefault(x => x.Integrations.Any(t => t.Integration.Value == originSubscription.Id));
        }

        /// <summary>
        /// Returns patient by integration id
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="vendor"></param>
        /// <returns></returns>
        private async Task<Patient?> GetPatientAsync(string integrationId, IntegrationVendor vendor)
        {
            try
            {
                return await _patientsService.GetByIntegrationIdAsync(integrationId, vendor);
            }
            catch (AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Ignore case when patient does not exist in clarity
                return null;
            }
        }

        /// <summary>
        /// Returns corresponding payment price
        /// </summary>
        /// <param name="originSubscription"></param>
        /// <param name="vendor"></param>
        /// <returns></returns>
        private async Task<PaymentPrice?> GetCorrespondingPaymentPriceAsync(SubscriptionIntegrationModel originSubscription, IntegrationVendor vendor)
        {
            var productIntegrationId = originSubscription.ProductIntegrationId;
            var amountInDollars = Convert.ToDecimal(originSubscription.RateInCents) / 100.0M;
            var interval = originSubscription.Interval;
            var intervalCount = originSubscription.IntervalCount;
            
            try
            {
                return await _paymentPlansService.GetPaymentPriceByRecurringDetails(
                    integrationId: productIntegrationId,
                    vendor: vendor,
                    purpose: IntegrationPurposes.Payment.ProductId,
                    amountInDollars: amountInDollars,
                    interval: interval,
                    intervalCount: intervalCount);
            }
            catch (AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Ignore case when price does not exist in clarity
                return null;
            }
        }

        /// <summary>
        /// Returns origin subscription payment strategy
        /// </summary>
        /// <param name="originSubscription"></param>
        /// <returns></returns>
        private PaymentStrategy GetOriginSubscriptionPaymentStrategy(SubscriptionIntegrationModel originSubscription)
        {
            return originSubscription.PeriodInMonths switch
            {
                1 => PaymentStrategy.PartialPayment,
                12 => PaymentStrategy.FullPayment,
                _ => throw new ArgumentException("Unsupported period")
            };
        }

        #endregion
    }
}