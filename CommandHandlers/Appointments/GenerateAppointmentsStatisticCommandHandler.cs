using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.Appointments.Flows;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Commands.Timezones;
using WildHealth.Application.Extensions;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Schedulers.Availability;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Extensions;
using WildHealth.Common.Models.Appointments;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.TimeKit.Clients.Constants;

namespace WildHealth.Application.CommandHandlers.Appointments;

public class GenerateAppointmentsStatisticCommandHandler : IRequestHandler<GenerateAppointmentsStatisticCommand, AppointmentsStatisticModel>
{
    private readonly IAppointmentsService _appointmentsService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISchedulerAvailabilityService _schedulerAvailabilityService;
    private readonly IMediator _mediator;
    private readonly IEmployeeService _employeeService;

    public GenerateAppointmentsStatisticCommandHandler(
        IAppointmentsService appointmentsService, 
        IDateTimeProvider dateTimeProvider, 
        ISchedulerAvailabilityService schedulerAvailabilityService,
        IMediator mediator,
        IEmployeeService employeeService)
    {
        _appointmentsService = appointmentsService;
        _dateTimeProvider = dateTimeProvider;
        _schedulerAvailabilityService = schedulerAvailabilityService;
        _mediator = mediator;
        _employeeService = employeeService;
    }

    public async Task<AppointmentsStatisticModel> Handle(GenerateAppointmentsStatisticCommand request, CancellationToken cancellationToken)
    {
        var employee = request.Employee ??
                       await _employeeService.GetByIdAsync(request.EmployeeId, EmployeeSpecifications.WithUser);
        var timeZone =
            await _mediator.Send(GetCurrentTimezoneCommand.ForEmployee(request.EmployeeId, employee.User.PracticeId),
                cancellationToken);

        var statisticsDateTime = request.AsOfDateTime ?? _dateTimeProvider.UtcNow();

        var currentDate = TimeZoneInfo.ConvertTimeFromUtc(statisticsDateTime, timeZone);
        var weekStartDate = currentDate.WeekDateRange(1);
        var weekEndDate = currentDate.WeekDateRange(7);
     
        var appointments = await _appointmentsService.GetEmployeeAppointmentsAsync(
            request.EmployeeId,
            startDate: weekStartDate, 
            endDate: weekEndDate,
            onlyActive: true);

        var available55MinuteMeetings =
            await _schedulerAvailabilityService.GetAvailabilityCountAsync(
                practiceId: employee.User.PracticeId, 
                schedulerAccountId: employee.SchedulerAccountId,
                from: weekStartDate,
                to: weekEndDate,
                duration: TimeKitConstants.TimeSpansInMinutes.Minutes60);
        
        var available25MinuteMeetings = 
            await _schedulerAvailabilityService.GetAvailabilityCountAsync(
                practiceId: employee.User.PracticeId, 
                schedulerAccountId: employee.SchedulerAccountId,
                from: weekStartDate,
                to: weekEndDate,
                duration: TimeKitConstants.TimeSpansInMinutes.Minutes30);

        var flow = new GetAppointmentsStatisticFlow(
            weekStart: weekStartDate,
            weekEnd: weekEndDate,
            appointments, 
            available25MinuteMeetings, 
            available55MinuteMeetings, 
            employee,
            currentDate);

        var result = flow.Execute();
        
        await _mediator.PublishAll(result.Notifications, cancellationToken);
        
        return result.Statistics;
    }
}