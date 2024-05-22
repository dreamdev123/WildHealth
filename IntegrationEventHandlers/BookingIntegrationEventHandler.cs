using System;
using MediatR;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Bookings;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.IntegrationEventHandlers.EventFactoryExtensions;
using WildHealth.IntegrationEvents.Bookings.Payloads;

namespace WildHealth.Application.IntegrationEventHandlers;

public class BookingIntegrationEventHandler : IEventHandler<BookingIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<BookingIntegrationEventHandler> _logger;

    public BookingIntegrationEventHandler(IMediator mediator, ILogger<BookingIntegrationEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(BookingIntegrationEvent @event)
    {
        try
        {
            var notification = CreateNotification(@event);
            await _mediator.Publish(notification);
        }
        catch (ArgumentOutOfRangeException aore)
        {
            //Fine.  It's just info.
            _logger.LogInformation(aore.ToString());
        }
    }

    private INotification CreateNotification(BookingIntegrationEvent @event)
    {
        return @event.PayloadType switch
        {
            nameof(ReminderBookingPayload) => (@event.Payload as ReminderBookingPayload ?? @event.DeserializePayload<ReminderBookingPayload>())
                .ToReminderSentEvent(),
            _ => throw new ArgumentOutOfRangeException($"Unknown booking payload type of {@event.PayloadFullType}")
        };
    }
}