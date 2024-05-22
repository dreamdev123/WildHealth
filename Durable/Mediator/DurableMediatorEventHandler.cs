using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Messages;
using WildHealth.IntegrationEvents.Messages.Payloads;

namespace WildHealth.Application.Durable.Mediator;

/// <summary>
/// Forwards ServiceBus events to MediatR handlers
/// </summary>
public class DurableMediatorEventHandler : IEventHandler<DurableMediatorEvent>
{
    private readonly IMediator _mediator;
    private readonly IEventBus _eventBus;
    private readonly IWebHostEnvironment _hostEnvironment;

    public DurableMediatorEventHandler(
        IMediator mediator, 
        IEventBus eventBus, 
        IWebHostEnvironment hostEnvironment)
    {
        _mediator = mediator;
        _eventBus = eventBus;
        _hostEnvironment = hostEnvironment;
    }

    public async Task Handle(DurableMediatorEvent @event)
    {
        var eventType = Type.GetType(@event.PayloadFullType);
        var notification = JsonConvert.DeserializeObject(@event.Payload.ToString(), eventType);
        try
        {
            await _mediator.Publish(notification);
        }
        catch(Exception e)
        {
            var (payloadType, payload) = (@event.PayloadFullType, @event.Payload);
            
            var errorMessage = $"Environment: {_hostEnvironment.EnvironmentName} " +
                               $"Something went wrong when processing '{payloadType}'. " +
                               $"Re-submit the message from the dead letter queue once the root cause is resolved. " +
                               $"Payload: '{JsonConvert.SerializeObject(payload)}'; Exception: {e}";
            
            var eventPayload = new SlackMessagePayload(
                message: errorMessage,
                messageType: "BackgroundProcessCrashed"
            );
            
            // notifying the system about the crash so it's acted upon
            await _eventBus.Publish(new MessageIntegrationEvent(eventPayload, DateTime.UtcNow));

            throw;
        }
    }
}