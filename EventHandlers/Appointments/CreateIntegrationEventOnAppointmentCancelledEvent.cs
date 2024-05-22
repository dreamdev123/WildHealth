using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Services.Users;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Enums.Employees;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Appointments;
using WildHealth.IntegrationEvents.Appointments.Payloads;

namespace WildHealth.Application.EventHandlers.Appointments;

public class CreateIntegrationEventOnAppointmentCancelledEvent : INotificationHandler<AppointmentCancelledEvent>
{
    private readonly IEventBus _eventBus;
    private readonly IUsersService _usersService;

    public CreateIntegrationEventOnAppointmentCancelledEvent(IUsersService usersService)
    {
        _usersService = usersService;
        _eventBus = EventBusProvider.Get();
    }

    public async Task Handle(AppointmentCancelledEvent notification, CancellationToken cancellationToken)
    {
        var appointment = notification.Appointment;
        var patient = appointment.Patient;
        if (patient is null)
        {
            return;
        }

        if (appointment.CancelledBy is null)
        {
            return;
        }

        var user = await _usersService.GetByIdAsync(appointment.CancelledBy.Value);

        var cancelledByEmail = user?.Email;

        var cancelledBy = appointment.CancelledBy == patient.UserId
            ? "patient"
            : "employee";
        
        switch (appointment.WithType)
        {
            case AppointmentWithType.HealthCoach:
                await _eventBus.Publish(new AppointmentIntegrationEvent(
                    payload: new CancelledHealthCoachAppointmentPayload(
                        purpose: appointment.Purpose.ToString(),
                        comment: appointment.Comment,
                        joinLink: appointment.JoinLink,
                        order: patient.Appointments.Count(a => a.WithType.Equals(AppointmentWithType.HealthCoach))
                            .ToString(),
                        date: appointment.StartDate.ToString(),
                        coachNames: appointment.Employees.Where(x => x.Employee.Type == EmployeeType.Coach)
                            .Select(x => x.Employee.User.GetFullname()),
                        providerNames: appointment.Employees.Where(x => x.Employee.Type == EmployeeType.Provider)
                            .Select(x => x.Employee.User.GetFullname()),
                        cancelledBy: cancelledBy,
                        cancelledByEmail: cancelledByEmail,
                        reason: appointment.CancellationReason.ToString(),
                        source: notification.Source
                    ),
                    patient: new PatientMetadataModel(patient.Id.GetValueOrDefault(), patient.User.UserId()),
                    eventDate: appointment.CreatedAt
                ), cancellationToken);
                break;
            case AppointmentWithType.Provider:
            case AppointmentWithType.HealthCoachAndProvider:
                await _eventBus.Publish(new AppointmentIntegrationEvent(
                    payload: new CancelledMedicalAppointmentPayload(
                        purpose: appointment.Purpose.ToString(),
                        comment: appointment.Comment,
                        joinLink: appointment.JoinLink,
                        order: patient.Appointments.Select(a =>
                            a.WithType.Equals(AppointmentWithType.HealthCoachAndProvider) ||
                            a.WithType.Equals(AppointmentWithType.Provider)).Count().ToString(),
                        date: appointment.StartDate.ToString(),
                        coachNames: appointment.Employees.Where(x => x.Employee.Type == EmployeeType.Coach)
                            .Select(x => x.Employee.User.GetFullname()),
                        providerNames: appointment.Employees.Where(x => x.Employee.Type == EmployeeType.Provider)
                            .Select(x => x.Employee.User.GetFullname()),
                        cancelledBy: cancelledBy,
                        cancelledByEmail: cancelledByEmail,
                        reason: appointment.CancellationReason.ToString(),
                        source: notification.Source
                    ),
                    patient: new PatientMetadataModel(patient.Id.GetValueOrDefault(), patient.User.UserId()),
                    eventDate: appointment.CreatedAt
                ), cancellationToken);
                break;
        }
    }
}