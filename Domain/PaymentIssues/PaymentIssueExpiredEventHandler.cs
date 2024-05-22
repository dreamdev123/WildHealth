using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Integration.Factories.IntegrationServiceFactory;

namespace WildHealth.Application.Domain.PaymentIssues;

public class PaymentIssueExpiredEventHandler : INotificationHandler<PaymentIssueExpiredEvent>
{
    private readonly MaterializeFlow _materializer;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IIntegrationServiceFactory _integrationServiceFactory;
    
    public PaymentIssueExpiredEventHandler(
        MaterializeFlow materializeFlow, 
        ISubscriptionService subscriptionService, 
        IIntegrationServiceFactory integrationServiceFactory)
    {
        _materializer = materializeFlow;
        _subscriptionService = subscriptionService;
        _integrationServiceFactory = integrationServiceFactory;
    }

    public async Task Handle(PaymentIssueExpiredEvent notification, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionService.GetByPaymentIssueId(notification.PaymentIssueId);
        
        await new CancelSubscriptionFlow(
            subscription,
            CancellationReasonType.PaymentFailed,
            string.Empty,
            DateTime.UtcNow,
            Array.Empty<PaymentIssue>()).Materialize(_materializer);

        var integrationService = await _integrationServiceFactory.CreateAsync(subscription.PracticeId!.Value);
        await integrationService.TryCancelSubscriptionAsync(subscription, DateTime.UtcNow, "payment overdue");   
    }
}