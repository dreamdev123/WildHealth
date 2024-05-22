using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Shared.Enums;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Patients;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Appointments;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Models.Appointments;

namespace WildHealth.Application.EventHandlers.Appointments
{
    public class SendNotificationOnAppointmentConfirmedEvent : INotificationHandler<AppointmentCreatedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<SendNotificationOnAppointmentConfirmedEvent> _logger;
        private readonly IAppointmentsService _appointmentsService;
        
        public SendNotificationOnAppointmentConfirmedEvent(INotificationService notificationService,
            ILogger<SendNotificationOnAppointmentConfirmedEvent> logger, 
            IAppointmentsService appointmentsService)
        {
            _notificationService = notificationService;
            _logger = logger;
            _appointmentsService = appointmentsService;
        }

        public async Task Handle(AppointmentCreatedEvent @event, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentsService.GetByIdAsync(@event.AppointmentId);
            
            var patient = appointment.Patient;
            var employee = appointment.Employees.Select(c => c.Employee).First();

            if (patient is null)
            {
                return;
            }
            
            switch (@event.CreatedBy)
            {
                case UserType.Employee:
                    await SendNotificationToPatient(patient, employee, appointment);
                    break;
                case UserType.Patient: 
                    break;
                default: 
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        #region private

        private async Task SendNotificationToPatient(Patient patient, Employee employee, Appointment appointment)
        {
            var appointmentDomain = AppointmentDomain.Create(appointment);
            
            _logger.LogInformation("Sending Appointment Confirmation Notification");
            var newNotification = new AppointmentConfirmedNotification(
                patient: patient,
                employee: employee,
                timezonedDate: appointmentDomain.GetTimeZoneStartTime(true),
                timezoneName: appointmentDomain.GetTimezoneDisplayName(forPatient: true)
            );

            try
            {
                await _notificationService.CreateNotificationAsync(newNotification);
            }
            catch (Exception err)
            {
                _logger.LogError($"[SendNotificationOnAppointmentConfirmedEvent] error creating notification: {err.ToString()} ");
                // this error shows up in UI, we don't want that
                // throw new ApplicationException("[SendNotificationOnAppointmentConfirmedEvent] error creating notification");
            }

        }
        
        #endregion
    }
}

