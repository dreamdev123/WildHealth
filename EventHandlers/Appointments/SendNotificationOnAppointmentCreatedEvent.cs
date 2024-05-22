using System;
using System.Collections.Generic;
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
using WildHealth.Application.Services.Appointments;

namespace WildHealth.Application.EventHandlers.Appointments
{
    public class SendNotificationOnAppointmentCreatedEvent : INotificationHandler<AppointmentCreatedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IAppointmentsService _appointmentsService;

        public SendNotificationOnAppointmentCreatedEvent(
            INotificationService notificationService, 
            IAppointmentsService appointmentsService)
        {
            _notificationService = notificationService;
            _appointmentsService = appointmentsService;
        }

        public async Task Handle(AppointmentCreatedEvent @event, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentsService.GetByIdAsync(@event.AppointmentId);
            
            var patient = appointment.Patient;
            var employees = appointment.Employees.Select(c => c.Employee);

            if (patient is null)
            {
                return;
            }
            
            switch (@event.CreatedBy)
            {
                case UserType.Patient:
                    await SendNotificationToEmployees(patient, employees);
                    break;
                case UserType.Employee: 
                    break;
                default: 
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        #region private

        private async Task SendNotificationToEmployees(Patient patient, IEnumerable<Employee> employees)
        {
            foreach (var employee in employees)
            {
                var newNotification = new AppointmentRequestNotification(
                    patient: patient,
                    employee: employee
                );

                await _notificationService.CreateNotificationAsync(newNotification);
            }
        }
        
        #endregion
    }
}
