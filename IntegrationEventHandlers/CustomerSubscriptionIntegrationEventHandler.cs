using System;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Integration.Events;
using WildHealth.Integration.Models.Subscriptions;
using WildHealth.IntegrationEvents.Payment;
using WildHealth.IntegrationEvents.Payment.Payloads;

namespace WildHealth.Application.IntegrationEventHandlers;

public class CustomerSubscriptionIntegrationEventHandler : IEventHandler<CustomerSubscriptionIntegrationEvent>
{
    private static IntegrationVendor IntegrationVendor => IntegrationVendor.Stripe;
    
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ILogger<CustomerSubscriptionIntegrationEventHandler> _logger;

    public CustomerSubscriptionIntegrationEventHandler(IMediator mediator, 
        ILogger<CustomerSubscriptionIntegrationEventHandler> logger, 
        IMapper mapper)
    {
        _mediator = mediator;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task Handle(CustomerSubscriptionIntegrationEvent @event)
    {
        var notification = await CreateNotification(@event);
        _logger.LogWarning("Event received but not handled {Notification}", notification);
        
        // TODO: uncomment this when we get rid of IntegrationsController. CLAR-3725
        // await _mediator.Publish(notification);
    }

    private Task<INotification> CreateNotification(CustomerSubscriptionIntegrationEvent @event)
    {
        return Task.FromResult<INotification>(@event.PayloadType switch
        {
            nameof(CustomerSubscriptionUpdatedPayload) => new IntegrationSubscriptionUpdatedEvent(
                subscription: _mapper.Map<SubscriptionIntegrationModel>(@event.DeserializePayload<CustomerSubscriptionUpdatedPayload>()),
                vendor: IntegrationVendor),
            nameof(CustomerSubscriptionCreatedPayload) => new IntegrationSubscriptionUpdatedEvent(
                subscription: _mapper.Map<SubscriptionIntegrationModel>(@event.DeserializePayload<CustomerSubscriptionCreatedPayload>()),
                vendor: IntegrationVendor),
            _ => throw new ArgumentOutOfRangeException($"Handler for Stripe event with [Type] = {@event.PayloadType} is not implemented")
        });
    }
}