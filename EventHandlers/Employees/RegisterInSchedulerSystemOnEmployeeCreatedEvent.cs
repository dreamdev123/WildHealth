using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.SchedulerSystem;
using WildHealth.Application.Events.Employees;
using MediatR;

namespace WildHealth.Application.EventHandlers.Employees
{
    public class RegisterInSchedulerSystemOnEmployeeCreatedEvent : INotificationHandler<EmployeeCreatedEvent>
    {
        private readonly IMediator _mediator;

        public RegisterInSchedulerSystemOnEmployeeCreatedEvent(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        public async Task Handle(EmployeeCreatedEvent notification, CancellationToken cancellationToken)
        {
            if (!notification.RegisterInSchedulerSystem)
            {
                return;
            }
            
            var command = new RegisterInSchedulerSystemCommand(notification.EmployeeId);
            
            await _mediator.Send(command, cancellationToken);
        }
    }
}