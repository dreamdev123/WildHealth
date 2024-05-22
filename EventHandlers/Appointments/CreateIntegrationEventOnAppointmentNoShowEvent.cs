using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Appointments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Appointments;
using WildHealth.IntegrationEvents.Appointments.Payloads;
using MediatR;

namespace WildHealth.Application.EventHandlers.Appointments
{
    public class CreateIntegrationEventOnAppointmentNoShowEvent : INotificationHandler<AppointmentNoShowEvent>
    {
        private readonly IEventBus _eventBus;

        public CreateIntegrationEventOnAppointmentNoShowEvent()
        {
            _eventBus = EventBusProvider.Get();
        }

        public async Task Handle(AppointmentNoShowEvent notification, CancellationToken cancellationToken)
        {
            var appointment = notification.Appointment;
            var patient = appointment.Patient;

            switch(appointment.WithType)
            {
                case AppointmentWithType.HealthCoach:
                    await _eventBus.Publish(new AppointmentIntegrationEvent(
                        payload: new NoShowHealthCoachAppointmentPayload(
                            purpose: appointment.Purpose.ToString(), 
                            comment: appointment.Comment, 
                            joinLink: appointment.JoinLink, 
                            order: patient.Appointments.Count(a => a.WithType.Equals(AppointmentWithType.HealthCoach)).ToString()
                            ),
                        patient: new PatientMetadataModel(patient.Id.GetValueOrDefault(), patient.User.UserId()),
                        eventDate: appointment.CreatedAt
                        ), cancellationToken);
                    break;
                case AppointmentWithType.Provider:
                case AppointmentWithType.HealthCoachAndProvider:
                    await _eventBus.Publish(new AppointmentIntegrationEvent(
                        payload: new NoShowMedicalAppointmentPayload(
                            purpose: appointment.Purpose.ToString(), 
                            comment: appointment.Comment, 
                            joinLink: appointment.JoinLink, 
                            order: patient.Appointments.Select(a => 
                                a.WithType.Equals(AppointmentWithType.HealthCoachAndProvider) ||
                                a.WithType.Equals(AppointmentWithType.Provider)).Count().ToString()
                            ),
                        patient: new PatientMetadataModel(patient.Id.GetValueOrDefault(), patient.User.UserId()),
                        eventDate: appointment.CreatedAt
                        ), cancellationToken);   
                    break;
            }
        }
    }
}

