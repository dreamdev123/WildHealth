using WildHealth.Domain.Entities.Orders;
using MediatR;

namespace WildHealth.Application.Events.Orders
{
    public class OrderCreatedEvent : INotification
    {
        public Order Order { get; }
        
        public OrderCreatedEvent(Order order)
        {
            Order = order;
        }
    }
}