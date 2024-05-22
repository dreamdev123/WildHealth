using System;
using System.Collections.Generic;
using MediatR;
using WildHealth.Application.Events.Appointments;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Employees;
using WildHealth.TimeKit.Clients.Constants;

namespace WildHealth.Application.CommandHandlers.Appointments.Flows;

public class GetAppointmentsStatisticFlow
{
    private readonly IEnumerable<Appointment> _appointments;
    private readonly int _available25MinuteMeetings;
    private readonly int _available55MinuteMeetings;
    private readonly Employee _employee;
    private readonly DateTime _utcNow;
    private readonly DateTime _weekStart;
    private readonly DateTime _weekEnd;

    private const int MinutesPerWeek = 2400;
    
    public GetAppointmentsStatisticFlow(
        DateTime weekStart,
        DateTime weekEnd,
        IEnumerable<Appointment> appointments, 
        int available25MinuteMeetings, 
        int available55MinuteMeetings, 
        Employee employee, 
        DateTime utcNow)
    {
        _weekStart = weekStart;
        _weekEnd = weekEnd;
        _appointments = appointments;
        _available25MinuteMeetings = available25MinuteMeetings;
        _available55MinuteMeetings = available55MinuteMeetings;
        _employee = employee;
        _utcNow = utcNow;
    }

    public GetAppointmentsStatisticFlowlowResult Execute()
    {
        var statisticModel = new AppointmentsStatisticModel
        {
            WeekStart = _weekStart,
            WeekEnd = _weekEnd,
            EmployeeId = _employee.GetId(),
            Available25MinuteMeetings = _available25MinuteMeetings,
            Available55MinuteMeetings = _available55MinuteMeetings
        };
        
        var totalVisitMinutes = 0;
        var roundedVisitMinutes = 0;
        foreach (var appointment in _appointments)
        {
            switch (appointment.Duration)
            {
                case <= 20:
                    statisticModel.Booked15MinuteMeetings++;
                    totalVisitMinutes += 20;
                    roundedVisitMinutes += 30;
                    break;
                case <= 30:
                    statisticModel.Booked25MinuteMeetings++;
                    totalVisitMinutes += 30;
                    roundedVisitMinutes += 30;
                    break;
                case <= 60:
                    statisticModel.Booked55MinuteMeetings++;
                    totalVisitMinutes += 60;
                    roundedVisitMinutes += 60;
                    break;
            }
        }
        
        statisticModel.Available15MinuteMeetings = statisticModel.Available25MinuteMeetings;

        statisticModel.VisitHours = FormatTimeFromMinutes(totalVisitMinutes);
        statisticModel.VisitHoursDecimal = FormatTimeDecimalFromMinutes(totalVisitMinutes);
        
        var totalAvailableMinutes =
            statisticModel.Available25MinuteMeetings * TimeKitConstants.TimeSpansInMinutes.Minutes30;
        statisticModel.AvailableHours = FormatTimeFromMinutes(totalAvailableMinutes);
        statisticModel.AvailableHoursDecimal = FormatTimeDecimalFromMinutes(totalAvailableMinutes);

        var totalAdministrativeMinutes = totalAvailableMinutes + totalVisitMinutes;
        statisticModel.AdministrativeHours = FormatTimeFromMinutes(totalAdministrativeMinutes);
        statisticModel.AdministrativeHoursDecimal = FormatTimeDecimalFromMinutes(totalAdministrativeMinutes);

        var unavailableMinutes = MinutesPerWeek - roundedVisitMinutes - totalAvailableMinutes;
        statisticModel.BlockedHours = FormatTimeFromMinutes(unavailableMinutes);
        statisticModel.BlockedHoursDecimal = FormatTimeDecimalFromMinutes(unavailableMinutes);

        return new GetAppointmentsStatisticFlowlowResult(statisticModel, GetNotifications(statisticModel));
    }
    
    private IEnumerable<INotification> GetNotifications(AppointmentsStatisticModel payload)
    {
        yield return new AppointmentsStatisticGeneratedEvent(payload, _employee, _utcNow);
    }
    private static string FormatTimeFromMinutes(int minutes)
    {
        var interval = GetTimeSpanFromMinutes(minutes);
        return $"{(int) interval.TotalHours}:{interval.Minutes.ToString("00")}";
    }
    private static decimal FormatTimeDecimalFromMinutes(int minutes)
    {
        var interval = GetTimeSpanFromMinutes(minutes);
        return (decimal)interval.TotalHours;
    }
    private static TimeSpan GetTimeSpanFromMinutes(int minutes)
    {
        if (minutes < 0)
        {
            minutes = 0;
        }
        return TimeSpan.FromMinutes(minutes);
    }
}

public record GetAppointmentsStatisticFlowlowResult(
    AppointmentsStatisticModel Statistics, 
    IEnumerable<INotification> Notifications);