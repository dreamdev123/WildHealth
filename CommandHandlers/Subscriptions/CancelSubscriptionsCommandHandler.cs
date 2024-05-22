using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.CommandHandlers.Subscriptions.Base;
using MediatR;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.PaymentIssues;

namespace WildHealth.Application.CommandHandlers.Subscriptions
{
    /// <summary>
    /// Provides cancel subscription command handler
    /// </summary>
    public class CancelSubscriptionsCommandHandler : BaseCancelSubscriptionsCommandHandler, IRequestHandler<CancelSubscriptionsCommand, Subscription>
    {
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly ILogger<CancelSubscriptionsCommandHandler> _logger;
        private readonly IPaymentIssuesService _paymentIssuesService;

        public CancelSubscriptionsCommandHandler(
            ISubscriptionService subscriptionService,
            IIntegrationServiceFactory integrationServiceFactory,
            IPermissionsGuard permissionsGuard,
            IMediator mediator,
            ILogger<CancelSubscriptionsCommandHandler> logger,
            MaterializeFlow materializeFlow, 
            IPaymentIssuesService paymentIssuesService) : base(subscriptionService, integrationServiceFactory, mediator, materializeFlow, paymentIssuesService)
        {
            _permissionsGuard = permissionsGuard;
            _logger = logger;
            _paymentIssuesService = paymentIssuesService;
        }
        
        /// <summary>
        /// Handles cancel subscription command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Subscription> Handle(CancelSubscriptionsCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Cancellation of subscription with [Id] {command.Id} has been started.");

            var subscription = await GetSubscriptionAsync(command.Id);
            var patient = subscription.Patient;

            AssertSubscriptionIsActive(subscription);

            if (command.Date is null)
            {
                subscription = await CancelSubscriptionAsync(
                    subscription: subscription, 
                    type: command.ReasonType,
                    reason: command.Reason
                );
            }
            else
            {
                subscription = await ScheduleCancellationAsync(
                    subscription: subscription, 
                    reasonType: command.ReasonType,
                    reason: command.Reason,
                    date: command.Date.Value
                );
            }

            await Mediator.Publish(new PatientUpdatedEvent(patient.GetId(), Enumerable.Empty<int>()), cancellationToken);

            _logger.LogInformation($"Cancellation of subscription with [Id] {command.Id} has been finished.");

            return subscription!;
        }

        #region private 

        private async Task<Subscription> GetSubscriptionAsync(int subscriptionId)
        {
            var subscription = await SubscriptionService.GetAsync(subscriptionId);

            _permissionsGuard.AssertPermissions(subscription);

            return subscription;
        }

        private void AssertSubscriptionIsActive(Subscription subscription)
        {
            if (subscription.GetStatus() != SubscriptionStatus.Active)
            {
                _logger.LogError($"Cancellation of subscription with [Id] {subscription.Id} has been failed. Subscription is not active.");

                throw new AppException(HttpStatusCode.BadRequest, "Can't cancel not active subscription.");
            }
        }

        private Task<Subscription> ScheduleCancellationAsync(Subscription subscription, CancellationReasonType reasonType, string reason, DateTime date)
        {
            return SubscriptionService.ScheduleCancellationAsync(
                subscription: subscription, 
                cancellationType: reasonType,
                cancellationReason: reason, 
                cancellationDate: date
            );
        }

        #endregion
    }
}