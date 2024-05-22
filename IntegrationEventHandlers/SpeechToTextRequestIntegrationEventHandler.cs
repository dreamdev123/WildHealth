using System;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Durable.Mediator;
using WildHealth.Application.IntegrationEventHandlers.EventFactoryExtensions;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Meetings;
using WildHealth.IntegrationEvents.Meetings.Payloads;
using WildHealth.IntegrationEvents.SpeechToTextRequests;
using WildHealth.IntegrationEvents.SpeechToTextRequests.Payloads;

namespace WildHealth.Application.IntegrationEventHandlers;

public class SpeechToTextRequestIntegrationEventHandler : IEventHandler<SpeechToTextRequestIntegrationEvent>
{
    private readonly IMediator _mediator;

    public SpeechToTextRequestIntegrationEventHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(SpeechToTextRequestIntegrationEvent @event)
    {
        var notification = CreateNotification(@event);
        
        await _mediator.Publish(notification);
    }

    private INotification CreateNotification(SpeechToTextRequestIntegrationEvent @event)
    {
        return @event.PayloadType switch {
            nameof(TranscriptionStatusChangedPayload) => (@event.Payload as TranscriptionStatusChangedPayload ?? @event.DeserializePayload<TranscriptionStatusChangedPayload>())
                .ToTranscriptionStatusChangedEvent(),
            _ => throw new ArgumentOutOfRangeException($"Handler for MeetingIntegrationEvent event with [Type] = {@event.PayloadType} is not implemented")
        };
    }
}