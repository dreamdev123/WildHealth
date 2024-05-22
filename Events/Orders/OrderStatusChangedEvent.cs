using WildHealth.Domain.Entities.Orders;
using MediatR;

namespace WildHealth.Application.Events.Orders
{
    public class OrderStatusChangedEvent : INotification
    {
        public Order Order { get; }
        
        public OrderStatusChangedEvent(Order order)
        {
            Order = order;
        }
    }
}