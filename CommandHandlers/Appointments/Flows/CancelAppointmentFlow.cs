using System;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Models.Appointments;

namespace WildHealth.Application.CommandHandlers.Appointments.Flows;

public class CancelAppointmentFlow : IMaterialisableFlow
{
    private readonly Appointment _appointment;
    private readonly User? _cancelledBy;
    private readonly DateTime _cancelledAt;
    private readonly string _patientProfileUrl;
    private readonly AppointmentCancellationReason _reason;

    public CancelAppointmentFlow(
        Appointment appointment, 
        User? cancelledBy, 
        DateTime cancelledAt, 
        string patientProfileUrl,
        AppointmentCancellationReason reason)
    {
        _appointment = appointment;
        _cancelledBy = cancelledBy;
        _cancelledAt = cancelledAt;
        _patientProfileUrl = patientProfileUrl;
        _reason = reason;
    }

    public MaterialisableFlowResult Execute()
    {
        var domain = AppointmentDomain.Create(_appointment);
        
        domain.Cancel(
            cancelledBy: _cancelledBy?.GetId(), 
            cancelledAt: _cancelledAt,
            cancellationReason: _reason
        );

        return _appointment.Updated() + RaiseNotifications();
    }

    private AppointmentCancelledNotification[] RaiseNotifications()
    {
        if (_appointment.Patient is null)
        {
            return Array.Empty<AppointmentCancelledNotification>();
        }
        
        return _appointment.Employees.Select(e => new AppointmentCancelledNotification(
            patient: _appointment.Patient,
            employee: e.Employee,
            cancelledBy: _cancelledBy,
            appointment: _appointment,
            patientProfileUrl: _patientProfileUrl
        )).ToArray();
    }
}