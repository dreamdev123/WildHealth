using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Constants;

namespace WildHealth.Application.CommandHandlers.Appointments;

public class GenerateAppointmentsStatisticsBatchCommandHandler : IRequestHandler<GenerateAppointmentsStatisticsBatchCommand>
{
    private readonly IEmployeeService _employeeService;
    private readonly IMediator _mediator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger _logger;

    public GenerateAppointmentsStatisticsBatchCommandHandler(
        IEmployeeService employeeService, 
        IMediator mediator, 
        IDateTimeProvider dateTimeProvider,
        ILogger<GenerateAppointmentsStatisticsBatchCommandHandler> logger)
    {
        _employeeService = employeeService;
        _mediator = mediator;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task Handle(GenerateAppointmentsStatisticsBatchCommand request, CancellationToken cancellationToken)
    {
        var employees = await _employeeService.GetByRolesIdsAsync(Roles.ProviderId, Roles.CoachId);

        // Want to create statistics for current week, as well as the next 4 weeks
        // Get a list of dates for today and the next 4 weeks to process
        var today = _dateTimeProvider.UtcNow();
        var datesOverNextMonth = GetFutureWeeks(today, 4);

        foreach (var employee in employees)
        {
            foreach (var date in datesOverNextMonth)
            {
                try
                {
                    await _mediator.Send(new GenerateAppointmentsStatisticCommand(employee.GetId(), date), cancellationToken);
                }
                catch(Exception ex)
                {
                    // Log errors and continue processing other employees
                    _logger.LogWarning(ex,
                        "Error while generating Appointments Statistic for [EmployeeId] {EmployeeId} with [date] {ProcessDate}. {ExceptionMessage}",
                        employee.Id, date.ToString(), ex);
                }
            }
        }
    }

    private static IEnumerable<DateTime> GetFutureWeeks(DateTime startDate, int numWeeks)
    {
        var dates = new List<DateTime>();
        // Start at 0 to include current day, add entry for number of weeks
        for (int week = 0; week <= numWeeks; week ++)
        {
            var nextDate = startDate.AddDays(7 * week);
            dates.Add(nextDate);
        }
        return dates;
    }
}