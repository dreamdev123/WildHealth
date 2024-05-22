using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Exceptions;

namespace WildHealth.Application.CommandHandlers.Orders.Flows;

public class DeleteOtherOrderFlow : IMaterialisableFlow
{
    private readonly OtherOrder _order;

    public DeleteOtherOrderFlow(OtherOrder order)
    {
        _order = order;
    }

    public MaterialisableFlowResult Execute()
    {
        if (_order.Status > OrderStatus.UnderReview)
        {
            throw new DomainException("Order can not be deleted.");
        }

        return _order.Deleted();
    }
}