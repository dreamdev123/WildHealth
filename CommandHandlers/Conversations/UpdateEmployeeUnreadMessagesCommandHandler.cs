using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Services.Employees;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class UpdateEmployeeUnreadMessagesCommandHandler : IRequestHandler<UpdateEmployeeUnreadMessagesCommand>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IEmployeeService _employeeService;
    private readonly IPracticeService _practiceService;
    
    
    public UpdateEmployeeUnreadMessagesCommandHandler(
        ILogger<UpdateConversationUnreadMessagesCommandHandler> logger, 
        IMediator mediator,
        IEmployeeService employeeService,
        IPracticeService practiceService)
    {
        _logger = logger;
        _mediator = mediator;
        _employeeService = employeeService;
        _practiceService = practiceService;
    }

    public async Task Handle(UpdateEmployeeUnreadMessagesCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Update employee conversation participant unread messages has started");

        var activePractices = await _practiceService.GetActiveAsync();

        foreach (var practice in activePractices)
        {
            _logger.LogInformation($"Update employee conversation participant unread messages for [PracticeId] = {practice.GetId()} has started");
            
            var employees = await _employeeService.GetAllPracticeEmployeesAsync(practice.GetId());

            foreach (var employee in employees)
            {
                try
                {
                    await _mediator.Send(new UpdateConversationUnreadMessagesCommand(
                        user: employee.User
                    ), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error attempting ot update conversation unread messages for [EmployeeId] = {employee.GetId()} - {ex.ToString()}");
                }
            }
            
            _logger.LogInformation($"Update employee conversation participant unread messages for [PracticeId] = {practice.GetId()} has completed");
        }
        
        _logger.LogInformation($"Update employee conversation participant unread messages has completed");
    }
}