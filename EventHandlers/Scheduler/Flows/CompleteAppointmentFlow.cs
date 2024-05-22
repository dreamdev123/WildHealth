using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Models.Timeline;

namespace WildHealth.Application.EventHandlers.Scheduler.Flows;

public class CompleteAppointmentFlow : IMaterialisableFlow
{
    private readonly Appointment? _appointment;
    private readonly Employee? _employee;
    private readonly bool _completed;
    private readonly DateTime _utcNow;

    public CompleteAppointmentFlow(Appointment? appointment, Employee? employee, bool completed, DateTime utcNow)
    {
        _appointment = appointment;
        _employee = employee;
        _completed = completed;
        _utcNow = utcNow;
    }

    public MaterialisableFlowResult Execute()
    {
        if (_appointment?.PatientId is null || !_completed || _appointment.IsNoShow || _employee is null) 
            return MaterialisableFlowResult.Empty;
        
        var employeeName = new EmployeeName(_employee);
        var timelineEvent = new VisitCompletedTimelineEvent(_appointment.PatientId.Value,
            _utcNow,
            new VisitCompletedTimelineEvent.Data(_appointment.Purpose, employeeName.ToString(), _appointment.GetId()));

        return timelineEvent.Added();
    }
}