using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Commands.Tags;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Models.Payment;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Subscriptions
{
    public class ActivateSubscriptionCommandHandler : IRequestHandler<ActivateSubscriptionCommand, Subscription>
    {
        private readonly IPatientsService _patientsService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IIntegrationServiceFactory _integrationServiceFactory;
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly MaterializeFlow _materialization;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ActivateSubscriptionCommandHandler(
            IPatientsService patientsService, 
            IPermissionsGuard permissionsGuard, 
            IIntegrationServiceFactory integrationServiceFactory, 
            IPaymentPlansService paymentPlansService, 
            IPaymentService paymentService, 
            ILogger<ActivateSubscriptionCommandHandler> logger,
            IMediator mediator, 
            MaterializeFlow materialization, IDateTimeProvider dateTimeProvider)
        {
            _patientsService = patientsService;
            _permissionsGuard = permissionsGuard;
            _integrationServiceFactory = integrationServiceFactory;
            _paymentPlansService = paymentPlansService;
            _paymentService = paymentService;
            _logger = logger;
            _mediator = mediator;
            _materialization = materialization;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Subscription> Handle(ActivateSubscriptionCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Activating subscription for patient with [Id] {command.PatientId} has been started.");

            var utcNow = _dateTimeProvider.UtcNow();
            
            var patient = await _patientsService.GetByIdAsync(command.PatientId);

            var recentSubscription = patient.MostRecentSubscription;
            
            _permissionsGuard.AssertPermissions(patient);
            
            var paymentPrice = await _paymentPlansService.GetPaymentPriceByIdAsync(command.PaymentPriceId);

            var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);
            
            var subscriptionPrice = SubscriptionPriceDomain.Create(null, paymentPrice, null, utcNow, recentSubscription.StartDate, false);
            
            var flow = new ActivateSubscriptionFlow(
                patient: patient,
                paymentPrice: paymentPrice,
                previousSubscription: patient.MostRecentSubscription,
                startDate: command.StartDate,
                utcNow: utcNow
            );
            
            try
            {
                var subscription = await flow
                    .Materialize(_materialization)
                    .Select<Subscription>();
                    
                var originSubscription = await integrationService.CreateOrUpdateSubscriptionAsync(patient, subscriptionPrice, recentSubscription);

                await _paymentService.ProcessSubscriptionPaymentAsync(patient, originSubscription.Id);

                await new MarkSubscriptionAsPaidFlow(subscription, originSubscription.Id, integrationService.IntegrationVendor).Materialize(_materialization);
                
                // Remove tag if they activate
                await _mediator.Send(new RemoveTagCommand(patient, Common.Constants.Tags.NeedsActivation), cancellationToken);

                return subscription;
            }
            finally
            {
                // IMPORTANT: patient should be unlocked after transaction
                await _patientsService.UnlockPatientAsync(patient);
            }
        }
    }
}