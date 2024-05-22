using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Events.Employees;

namespace WildHealth.Application.EventHandlers.Employees
{
    public class SendInviteEmailOnEmployeeCreatedEvent : INotificationHandler<EmployeeCreatedEvent>
    {
        private readonly IMediator _mediator;

        public SendInviteEmailOnEmployeeCreatedEvent(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(EmployeeCreatedEvent notification, CancellationToken cancellationToken)
        {
            var command = new SendEmployeeInviteCommand(notification.EmployeeId);
            
            await _mediator.Send(command, cancellationToken);
        }
    }
}