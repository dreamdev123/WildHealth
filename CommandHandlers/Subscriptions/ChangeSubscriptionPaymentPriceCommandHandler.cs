using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Application.CommandHandlers.Subscriptions.Base;
using WildHealth.Application.Commands.Products;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;
using WildHealth.Application.CommandHandlers.Payments.Flows;
using WildHealth.Application.CommandHandlers.Products.Flows;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Domain.PaymentIssues;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Services.PaymentIssues;
using WildHealth.Application.Services.Products;
using WildHealth.Application.Services.PromoCodes;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.CommandHandlers.Subscriptions
{
    /// <summary>
    /// Provides cancel subscription command handler
    /// </summary>
    public class ChangeSubscriptionPaymentPriceCommandHandler : BaseCancelSubscriptionsCommandHandler, IRequestHandler<ChangeSubscriptionPaymentPriceCommand, Subscription>
    {
        private const string CancellationReason = "Subscription was cancelled in change subscription payment price command handler";
        
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly ILogger<ChangeSubscriptionPaymentPriceCommandHandler> _logger;
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly IPatientsService _patientsService;
        private readonly IPaymentService _paymentService;
        private readonly IPromoCodeCouponsService _promoCodeService;
        private readonly MaterializeFlow _materialize;
        private readonly IPatientProductsService _patientProductsService;
        private readonly IProductsService _productsService;
        private readonly IPaymentIssueManager _paymentIssueManager;

        public ChangeSubscriptionPaymentPriceCommandHandler(
            ISubscriptionService subscriptionService,
            IIntegrationServiceFactory integrationServiceFactory,
            IPermissionsGuard permissionsGuard,
            IMediator mediator,
            ILogger<ChangeSubscriptionPaymentPriceCommandHandler> logger,
            IPaymentPlansService paymentPlansService,
            IPatientsService patientsService,
            IPaymentService paymentService, 
            IPromoCodeCouponsService promoCodeService, 
            MaterializeFlow materialize, 
            IPatientProductsService patientProductsService,
            MaterializeFlow materializeFlow, 
            IProductsService productsService, 
            IPaymentIssuesService paymentIssuesService, 
            IPaymentIssueManager paymentIssueManager) : base(subscriptionService, integrationServiceFactory, mediator, materializeFlow, paymentIssuesService)
        {
            _permissionsGuard = permissionsGuard;
            _logger = logger;
            _paymentPlansService = paymentPlansService;
            _patientsService = patientsService;
            _paymentService = paymentService;
            _promoCodeService = promoCodeService;
            _materialize = materialize;
            _patientProductsService = patientProductsService;
            _productsService = productsService;
            _paymentIssueManager = paymentIssueManager;
        }
        
        /// <summary>
        /// Handles cancel of a given subscription.  Can either be an active subscription or canceled subscription.
        /// Will create a new subscription for the target patient based on parameters passed in, most notably the paymentPriceId
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Subscription> Handle(ChangeSubscriptionPaymentPriceCommand command, CancellationToken cancellationToken)
        {
            return await HandleCore(command, cancellationToken);
        }
        
        private async Task<Subscription> HandleCore(ChangeSubscriptionPaymentPriceCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                $"Change of subscription with [Id] {command.CurrentSubscriptionId} to [PaymentPriceId] = {command.NewPaymentPriceId} has been started.");

            await _paymentIssueManager.ThrowIfHasOutstandingPayment(command.CurrentSubscriptionId); // ensure subscription doesn't have outstanding payments
            
            var targetSubscription = await GetSubscriptionAsync(command.CurrentSubscriptionId);
            var userPracticeId = targetSubscription.Patient.User.PracticeId;
            var patient = await _patientsService.GetByIdAsync(targetSubscription.PatientId, PatientSpecifications.PatientWithIntegrations);
            var integrationService = await IntegrationServiceFactory.CreateAsync(userPracticeId);
            var integrationSubscription = await integrationService.GetPatientSubscriptionAsync(patient, targetSubscription).ToOption();
            var startDate = command.StartDate ?? targetSubscription.StartDate;
            var endDate = command.EndDate ?? targetSubscription.EndDate;
            var paymentPrice = await _paymentPlansService.GetPaymentPriceByIdAsync(command.NewPaymentPriceId);
            var productsToExpire = await _patientProductsService.GetBuiltInByPatientAsync(patient.GetId());
            var allPracticeProducts = await _productsService.GetAsync(userPracticeId);
            var builtInPatientProducts = await _patientProductsService.GetBuiltInProductsForCurrentSubscription(targetSubscription.Patient.GetId());
            var integrationVendor = await _paymentService.GetIntegrationVendorAsync(targetSubscription.PracticeId ?? 0);
            var coupon = await _promoCodeService.GetAsync(command.CouponCode, userPracticeId);
            
            // Expire all built in patient products for that patient 
            await new ExpirePatientProductsFlow(productsToExpire, $"Updating subscription with a new [PaymentPriceId] = {paymentPrice.GetId()}", DateTime.UtcNow)
                .Materialize(_materialize);

            var newSubscription = (await new RenewSubscriptionFlow(
                targetSubscription,
                integrationSubscription.ValueOr(null),
                targetSubscription.Patient,
                paymentPrice,
                targetSubscription.EmployerProduct,
                coupon,
                integrationVendor, 
                DateTime.UtcNow,
                startDate,
                endDate).Materialize(_materialize)).Select<Subscription, EntityAction.Add>();
            
            // Create the built in patient products for that patient
            var createBuiltInProductsFlow =
                new CreateBuiltInProductsFlow(newSubscription, Array.Empty<PatientProduct>(), allPracticeProducts);
            
            // Make sure that if some products have already been used that we document that
            var resetProductsFlow =
                new ResetPatientProductsFlow(newSubscription, builtInPatientProducts, _logger.LogInformation);

            await createBuiltInProductsFlow.PipeTo(resetProductsFlow)
                .Materialize(_materialize);

            if (targetSubscription.GetPaymentFlow() != newSubscription.GetPaymentFlow())
            {
                var substituteProductsCommand = new SubstituteProductsCommand(
                    patientId: patient.GetId(),
                    oldPaymentFlow: targetSubscription.GetPaymentFlow(),
                    newPaymentFlow: newSubscription.GetPaymentFlow()
                );

                await Mediator.Send(substituteProductsCommand, cancellationToken);
            }

            _logger.LogInformation(
                $"Cancellation of subscription with [Id] {command.CurrentSubscriptionId} has been finished.");

            return newSubscription!;
        }
        
        #region private
        
        private async Task<Subscription> GetSubscriptionAsync(int subscriptionId)
        {
            var subscription = await SubscriptionService.GetAsync(subscriptionId);

            _permissionsGuard.AssertPermissions(subscription);

            return subscription;
        }

        #endregion
    }
}