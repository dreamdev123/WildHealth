using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.EmployeeReminders;
using WildHealth.Application.Services.EmployeeReminders;
using WildHealth.Domain.Entities.EmployeeReminders;

namespace WildHealth.Application.CommandHandlers.EmployeeReminders;

public class UpdateEmployeeReminderCommandHandler:IRequestHandler<UpdateEmployeeReminderCommand,EmployeeReminder>
{
    private readonly IEmployeeReminderService _employeeReminderService;

    public UpdateEmployeeReminderCommandHandler(IEmployeeReminderService employeeReminderService)
    {
        _employeeReminderService = employeeReminderService;
    }

    public async Task<EmployeeReminder> Handle(UpdateEmployeeReminderCommand request, CancellationToken cancellationToken)
    {
        var employeeReminder = await _employeeReminderService.GetByIdAsync(request.Id);

        employeeReminder.Description = request.Description;
        employeeReminder.Title = request.Title;
        employeeReminder.DateRemind = request.DateRemind;

        var result = await _employeeReminderService.UpdateAsync(employeeReminder);

        return result;
    }
}