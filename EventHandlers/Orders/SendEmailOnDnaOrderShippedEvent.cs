using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Orders;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Application.Commands.Orders;
using WildHealth.Domain.Entities.Orders;
using MediatR;

namespace WildHealth.Application.EventHandlers.Orders
{
    public class SendEmailOnDnaOrderShippedEvent : INotificationHandler<OrderStatusChangedEvent>
    {
        private readonly IMediator _mediator;

        public SendEmailOnDnaOrderShippedEvent(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(OrderStatusChangedEvent notification, CancellationToken cancellationToken)
        {
            var order = notification.Order;

            if (order.Type == OrderType.Dna && order.Status == OrderStatus.Shipping && order is DnaOrder dnaOrder)
            {
                var command = new SendDnaOrderShippedEmailCommand(dnaOrder);

                await _mediator.Send(command, cancellationToken);
            }
        }
    }
}