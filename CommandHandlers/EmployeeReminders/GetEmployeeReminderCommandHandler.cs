using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.EmployeeReminders;
using WildHealth.Application.Services.EmployeeReminders;
using WildHealth.Domain.Entities.EmployeeReminders;

namespace WildHealth.Application.CommandHandlers.EmployeeReminders;

public class GetEmployeeReminderCommandHandler:IRequestHandler<GetEmployeeReminderCommand,ICollection<EmployeeReminder>>
{
    private readonly IEmployeeReminderService _employeeReminderService;

    public GetEmployeeReminderCommandHandler(IEmployeeReminderService employeeReminderService)
    {
        _employeeReminderService = employeeReminderService;
    }

    public async Task<ICollection<EmployeeReminder>> Handle(GetEmployeeReminderCommand request, CancellationToken cancellationToken)
    {
        var (dateStart, dateEnd) = GetStartDateTimeEndDateTime(request.Date);

        var result = await _employeeReminderService.GetByEmployeeIdAsync(
            employeeId: request.EmployeeId, 
            dateTimeStart: dateStart, 
            dateTimeEnd: dateEnd);

        return result;
    }

    private (DateTime, DateTime) GetStartDateTimeEndDateTime(DateTime date)
    {
        var dateStartString = date.Year + " " + date.Month + " " + date.Day;

        var dateStart = DateTime.Parse(dateStartString);

        var dateEnd = dateStart.AddDays(1);
        
        return (dateStart, dateEnd);
    }
}