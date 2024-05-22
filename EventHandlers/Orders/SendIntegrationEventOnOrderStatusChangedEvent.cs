using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Orders;
using WildHealth.IntegrationEvents.Orders.Payloads;
using WildHealth.Application.Events.Orders;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Domain.Enums.Orders;
using MediatR;

namespace WildHealth.Application.EventHandlers.Orders
{
    public class SendIntegrationEventOnOrderStatusChangedEvent : INotificationHandler<OrderStatusChangedEvent>
    {
        private readonly IEventBus _eventBus;

        public SendIntegrationEventOnOrderStatusChangedEvent(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }
        
        public async Task Handle(OrderStatusChangedEvent notification, CancellationToken cancellationToken)
        {
            if(notification.Order.Status == OrderStatus.Completed) 
            {
                await _eventBus.Publish(new OrderIntegrationEvent(
                    payload: new LabOrderFinalizedPayload(
                        orderNumber: notification.Order.Number),
                    patient: new PatientMetadataModel(notification.Order.Patient.GetId(), notification.Order.Patient.User.UserId()),
                    eventDate: DateTime.Now
                ));
            }
        }
    }
}