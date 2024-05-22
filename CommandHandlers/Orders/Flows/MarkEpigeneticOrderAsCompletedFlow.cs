using System;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Orders;

namespace WildHealth.Application.CommandHandlers.Orders.Flows;

public record MarkEpigeneticOrderAsCompletedFlow(EpigeneticOrder Order, DateTime UtcNow) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (Order.Status == OrderStatus.Completed)
        {
            return MaterialisableFlowResult.Empty;
        }
        
        Order.ChangeStatus(OrderStatus.Completed, UtcNow);

        return Order.Updated() + new OrderStatusChangedEvent(Order);
    }
}