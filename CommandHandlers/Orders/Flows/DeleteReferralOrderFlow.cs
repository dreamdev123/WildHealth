using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Exceptions;

namespace WildHealth.Application.CommandHandlers.Orders.Flows;

public class DeleteReferralOrderFlow : IMaterialisableFlow
{
    private readonly ReferralOrder _order;

    public DeleteReferralOrderFlow(ReferralOrder order)
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