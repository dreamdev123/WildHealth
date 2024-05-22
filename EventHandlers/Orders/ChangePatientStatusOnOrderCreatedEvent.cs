using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Events.Orders;
using MediatR;

namespace WildHealth.Application.EventHandlers.Orders
{
    public class ChangePatientStatusOnOrderCreatedEvent : INotificationHandler<OrderCreatedEvent>
    {
        private readonly IMediator _mediator;

        public ChangePatientStatusOnOrderCreatedEvent(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
        {
            var order = notification.Order;
            
            var command = new SynchronizePatientStatusWithOrderStatusCommand(order);
            
            await _mediator.Send(command, cancellationToken);
        }
    }
}