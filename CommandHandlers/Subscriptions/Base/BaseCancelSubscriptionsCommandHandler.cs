using System;
using System.Threading.Tasks;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Integration.Services;
using MediatR;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.PaymentIssues;

namespace WildHealth.Application.CommandHandlers.Subscriptions.Base
{
    public class BaseCancelSubscriptionsCommandHandler
    {
        protected readonly IIntegrationServiceFactory IntegrationServiceFactory;
        protected readonly ISubscriptionService SubscriptionService;
        protected readonly IMediator Mediator;
        protected readonly MaterializeFlow MaterializeFlow;
        protected readonly IPaymentIssuesService PaymentIssuesService;

        protected BaseCancelSubscriptionsCommandHandler(
            ISubscriptionService subscriptionService,
            IIntegrationServiceFactory integrationServiceFactory,
            IMediator mediator, 
            MaterializeFlow materializeFlow, 
            IPaymentIssuesService paymentIssuesService)
        {
            IntegrationServiceFactory = integrationServiceFactory;
            SubscriptionService = subscriptionService;
            Mediator = mediator;
            MaterializeFlow = materializeFlow;
            PaymentIssuesService = paymentIssuesService;
        }

        protected async Task<IWildHealthIntegrationService> GetIntegrationService(Subscription subscription)
        {
            return await IntegrationServiceFactory.CreateAsync(subscription.Patient.User.PracticeId);
        }

        protected async Task<Subscription?> CancelSubscriptionAsync(
            Subscription? subscription, 
            CancellationReasonType type, 
            string reason)
        {
            if (subscription is null)
            {
                return null;
            }

            var patientPaymentIssues = await PaymentIssuesService.GetActiveAsync(subscription.PatientId);
            await new CancelSubscriptionFlow(
                subscription, 
                type, 
                reason, 
                DateTime.UtcNow,
                patientPaymentIssues
            ).Materialize(MaterializeFlow);

            // Do not cancel subscription in integration system if:
            // * There is no integration id
            // * subscription can be activated (CLAR-6866)
            if (subscription.IsLinkedWithIntegrationSystem() && !subscription.CanBeActivated())
            {
                var integrationService = await GetIntegrationService(subscription);

                await integrationService.TryCancelSubscriptionAsync(
                    subscription: subscription,
                    endDate: DateTime.UtcNow, 
                    reason: reason
                );
            }

            return subscription;
        }
    }
}