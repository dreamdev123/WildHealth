using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Events.Insurances;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Subscriptions;
using WildHealth.IntegrationEvents.Subscriptions.Payloads;

namespace WildHealth.Application.EventHandlers.Insurances;

public class SendIntegrationEventOnSubscriptionPaymentFlowChangedEvent : INotificationHandler<SubscriptionPaymentFlowChangedEvent>
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SendIntegrationEventOnSubscriptionPaymentFlowChangedEvent> _logger;

    public SendIntegrationEventOnSubscriptionPaymentFlowChangedEvent(
        ISubscriptionService subscriptionService,
        IEventBus eventBus,
        ILogger<SendIntegrationEventOnSubscriptionPaymentFlowChangedEvent> logger)
    {
        _subscriptionService = subscriptionService;
        _eventBus = eventBus;
        _logger = logger;
    }
    
    public async Task Handle(SubscriptionPaymentFlowChangedEvent notification, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionService.GetAsync(notification.SubscriptionId);
        
        var payload = new SubscriptionPaymentFlowChangedPayload(
            subscriptionId: notification.SubscriptionId,
            previousFlow: notification.PriorFlow,
            newFlow: notification.NewFlow);

        await _eventBus.Publish(new SubscriptionIntegrationEvent(
                payload: payload,
                patient: new PatientMetadataModel(subscription.Patient.GetId(), subscription.Patient.User.UserId()),
                eventDate: DateTime.UtcNow),
            cancellationToken);
    }
}