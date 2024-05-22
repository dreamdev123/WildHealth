using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Events.Employees;
using WildHealth.Application.Services.Employees;
using WildHealth.Northpass.Clients.Services;

namespace WildHealth.Application.EventHandlers.Employees;

public class CreateLearnerOnEmployeeCreatedEvent : INotificationHandler<EmployeeCreatedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CreateLearnerOnEmployeeCreatedEvent> _logger;
    private readonly INorthpassService _northpass;
    private readonly IEmployeeService _employeeService;

    public CreateLearnerOnEmployeeCreatedEvent (IMediator mediator,
        ILogger<CreateLearnerOnEmployeeCreatedEvent> logger,
        INorthpassService northpass,
        IEmployeeService employeeService)
    {
        _mediator = mediator;
        _logger = logger;
        _northpass = northpass;
        _employeeService = employeeService;
    }

    public async Task Handle(EmployeeCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Creating Northpass learner for employee {notification.EmployeeId}");
            var emp = await _employeeService.GetByIdAsync(notification.EmployeeId);
            var user = emp.User;
            var command = new CreateLearnerFromUserCommand(user);
            await _mediator.Send(command);
        }
        catch (Exception e)
        {
            var m =
                $"A Northpass Learner record could not be created for employee id {notification.EmployeeId}: {e.ToString()}";
            _logger.LogError(m);
        }
    }
}