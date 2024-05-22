using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.IntegrationEventHandlers.EventFactoryExtensions;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Scheduler;
using WildHealth.IntegrationEvents.Scheduler.Payloads;

namespace WildHealth.Application.IntegrationEventHandlers;

public class SchedulerIntegrationEventHandler : IEventHandler<SchedulerIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SchedulerIntegrationEventHandler> _logger;

    public SchedulerIntegrationEventHandler(IMediator mediator, ILogger<SchedulerIntegrationEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(SchedulerIntegrationEvent @event)
    {
        try
        {
            var notification = CreateNotification(@event);
            await _mediator.Publish(notification);
        }
        catch (ArgumentOutOfRangeException aore)
        {
            //It's fine.
            _logger.LogInformation(aore.ToString());
        }
    }
    
    private INotification CreateNotification(SchedulerIntegrationEvent @event)
    {
        return @event.PayloadType switch
        {
            nameof(SchedulerBookingCreatedPayload) => (@event.Payload as SchedulerBookingCreatedPayload ?? @event.DeserializePayload<SchedulerBookingCreatedPayload>())
                .ToBookingCreatedEvent(),
            nameof(SchedulerBookingCancelledPayload) => (@event.Payload as SchedulerBookingCancelledPayload ?? @event.DeserializePayload<SchedulerBookingCancelledPayload>())
                .ToBookingCancelledEvent(),
            nameof(SchedulerBookingCompletedPayload) => (@event.Payload as SchedulerBookingCompletedPayload ?? @event.DeserializePayload<SchedulerBookingCompletedPayload>())
                .ToBookingCompletedEvent(),
            _ => throw new ArgumentOutOfRangeException($"Unknown scheduler payload type of {@event.PayloadFullType}")
        };
    }
}