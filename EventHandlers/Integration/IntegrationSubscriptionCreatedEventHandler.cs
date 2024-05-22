using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Integration.Events;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Patients;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Integration.Models.Subscriptions;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.EmployerProducts;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Enums.Integrations;
using MediatR;

namespace WildHealth.Application.EventHandlers.Integration
{
    public class IntegrationSubscriptionCreatedEventHandler : INotificationHandler<IntegrationSubscriptionCreatedEvent>
    {
        private readonly IPatientsService _patientsService;
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly IEmployerProductService _employerProductService;
        private readonly ITransactionManager _transactionManager;
        private readonly IDateTimeProvider _dateTime;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly MaterializeFlow _materialization;

        public IntegrationSubscriptionCreatedEventHandler(
            IPatientsService patientsService,
            IPaymentPlansService paymentPlansService,
            IEmployerProductService employerProductService,
            ITransactionManager transactionManager,
            IDateTimeProvider dateTime,
            ILogger<IntegrationSubscriptionCreatedEventHandler> logger,
            IMediator mediator, 
            MaterializeFlow materialization)
        {
            _patientsService = patientsService;
            _paymentPlansService = paymentPlansService;
            _employerProductService = employerProductService;
            _transactionManager = transactionManager;
            _dateTime = dateTime;
            _logger = logger;
            _mediator = mediator;
            _materialization = materialization;
        }

        public async Task Handle(IntegrationSubscriptionCreatedEvent notification, CancellationToken cancellationToken)
        {
            var originSubscription = notification.Subscription;

            _logger.LogInformation($"Create membership from integration system for patient with [IntegrationId]: {originSubscription.PatientId} was started.");

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

            var paymentPrice = await GetCorrespondingPaymentPriceAsync(originSubscription, notification.Vendor);
            
            if (paymentPrice is null)
            {
                _logger.LogWarning($"Payment price with [IntegrationId]: {originSubscription.PlanId} does not exist.");

                return;
            }

            var employerProduct = await _employerProductService.GetByKeyAsync();

            var existingSubscription = GetExistingSubscription(patient, originSubscription);
            
            if (!(existingSubscription is null))
            {
                _logger.LogError($"Subscription with [IntegrationId]: {originSubscription.Id} is already exists.");
                
                return;
            }

            await using var transaction = _transactionManager.BeginTransaction();

            try
            {
                var newSubscription = await new CreateSubscriptionFlow(
                        patient: patient, 
                        paymentPrice: paymentPrice, 
                        employerProduct: employerProduct,
                        coupon: null, 
                        chargeStartupFee: false, 
                        utcNow: _dateTime.UtcNow())
                    .Materialize(_materialization)
                    .Select<Subscription>();

                // Create the built in patient products for that patient
                await _mediator.Send(new CreateBuiltInProductsCommand(newSubscription.GetId()));

                await new MarkSubscriptionAsPaidFlow(
                    newSubscription, 
                    originSubscription.Id, 
                    notification.Vendor).Materialize(_materialization);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation($"Create membership from integration system for patient with [IntegrationId]: {originSubscription.PatientId} was successfully ended.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create membership from integration system patient with [IntegrationId]: {originSubscription.PatientId} was failed. {ex}");
                
                await transaction.RollbackAsync(cancellationToken);
            }
        }
        
        #region private

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
            var integrationId = originSubscription.PlanId;
            var paymentStrategy = GetOriginSubscriptionPaymentStrategy(originSubscription);
            var price = GetOriginSubscriptionPrice(originSubscription);
            
            try
            {
                return await _paymentPlansService.GetPaymentPriceByIntegrationIdAsync(
                    integrationId: integrationId, 
                    vendor: vendor,
                    purpose: IntegrationPurposes.Payment.Id,
                    price: price, 
                    strategy: paymentStrategy
                );
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
            return originSubscription.Interval switch
            {
                "month" => PaymentStrategy.PartialPayment,
                "year" => PaymentStrategy.FullPayment,
                _ => throw new ArgumentException("Unsupported period")
            };
        }
        
        /// <summary>
        /// Returns origin subscription price
        /// </summary>
        /// <param name="originSubscription"></param>
        /// <returns></returns>
        private decimal GetOriginSubscriptionPrice(SubscriptionIntegrationModel originSubscription)
        {
            return new decimal((originSubscription.RateInCents ?? 0) / 100);
        }
        
        #endregion
    }
}