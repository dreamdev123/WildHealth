using System;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Durable.Mediator;
using WildHealth.Application.IntegrationEventHandlers.EventFactoryExtensions;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Meetings;
using WildHealth.IntegrationEvents.Meetings.Payloads;

namespace WildHealth.Application.IntegrationEventHandlers;

public class MeetingIntegrationEventHandler : IEventHandler<MeetingIntegrationEvent>
{
    private readonly IDurableMediator _mediator;

    public MeetingIntegrationEventHandler(IDurableMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(MeetingIntegrationEvent @event)
    {
        var notification = CreateNotification(@event);
        
        await _mediator.Publish(notification);
    }

    private INotification CreateNotification(MeetingIntegrationEvent @event)
    {
        return @event.PayloadType switch {
            nameof(RecordingTranscriptCompletedPayload) => (@event.Payload as RecordingTranscriptCompletedPayload ?? @event.DeserializePayload<RecordingTranscriptCompletedPayload>())
                .ToAppointmentTranscriptCompletedEvent(),
            _ => throw new ArgumentOutOfRangeException($"Handler for MeetingIntegrationEvent event with [Type] = {@event.PayloadType} is not implemented")
        };
    }
}