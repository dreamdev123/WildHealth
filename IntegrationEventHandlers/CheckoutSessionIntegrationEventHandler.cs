using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using WildHealth.Common.Constants;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Integration.Events;
using WildHealth.Integration.Models.Orders;
using WildHealth.IntegrationEvents.Payment;
using WildHealth.IntegrationEvents.Payment.Payloads;
using WildHealth.IntegrationEvents.Payment.Payloads.Models;
using WildHealth.Settings;

namespace WildHealth.Application.IntegrationEventHandlers;

public class CheckoutSessionIntegrationEventHandler : IEventHandler<CheckoutSessionIntegrationEvent>
{
    private static IntegrationVendor IntegrationVendor => IntegrationVendor.Stripe;
    
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ILogger<CheckoutSessionIntegrationEventHandler> _logger;
    private readonly ISettingsManager _settingsManager;

    public CheckoutSessionIntegrationEventHandler(IMapper mapper, 
        IMediator mediator, 
        ISettingsManager settingsManager, 
        ILogger<CheckoutSessionIntegrationEventHandler> logger)
    {
        _mapper = mapper;
        _mediator = mediator;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public async Task Handle(CheckoutSessionIntegrationEvent @event)
    {
        var notification = await CreateNotification(@event);

        _logger.LogWarning("Event received but not handled {Notification}", notification);
        
        // TODO: uncomment this when we get rid of IntegrationsController. CLAR-3725
        // await _mediator.Publish(notification);
    }
    
    private async Task<INotification> CreateNotification(CheckoutSessionIntegrationEvent @event)
    {
        return @event.PayloadType switch
        {
            nameof(CheckoutSessionCompletedPayload) => new IntegrationProductPurchasedEvent(
                order: _mapper.Map<OrderIntegrationModel>(await EnrichOrderItems(@event.DeserializePayload<CheckoutSessionCompletedPayload>(), @event.PracticeId)),
                vendor: IntegrationVendor),
            _ => throw new ArgumentOutOfRangeException($"Handler for Stripe event with [Type] = {@event.PayloadType} is not implemented")
        };
    }
    
    private async Task<CheckoutSessionCompletedPayload> EnrichOrderItems(CheckoutSessionCompletedPayload session, int practiceId)
    {
        if (session.OrderItems?.Any() ?? false)
        {
            return session;
        }

        StripeConfiguration.ApiKey = await GetKey(practiceId);

        var options = new SessionListLineItemsOptions();

        var service = new SessionService();

        var lineItems = await service.ListLineItemsAsync(session.Id, options);

        session.OrderItems = lineItems.Select(o => _mapper.Map<SessionOrderItem>(o)).ToArray();
        
        return session;
    }
    
    private async Task<string> GetKey(int practiceId)
    {
        return await _settingsManager.GetSetting<string>(SettingsNames.Payment.PrivateKey, practiceId);
    }
}