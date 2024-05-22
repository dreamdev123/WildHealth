using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using WildHealth.Application.Events.Appointments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Appointments;
using WildHealth.IntegrationEvents.Appointments.Payloads;
using MediatR;
using WildHealth.IntegrationEvents.Common.Payloads;

namespace WildHealth.Application.EventHandlers.Appointments
{
    public class CreateIntegrationEventOnAppointmentCompletedEvent : INotificationHandler<AppointmentCompletedEvent>
    {
        private readonly IEventBus _eventBus;
        private readonly IMapper _mapper;
        public CreateIntegrationEventOnAppointmentCompletedEvent(IMapper mapper)
        {
            _eventBus = EventBusProvider.Get();
            _mapper = mapper;
        }

        public async Task Handle(AppointmentCompletedEvent notification, CancellationToken cancellationToken)
        {
            var appointment = notification.Appointment;
            var patient = appointment.Patient;
            var user = patient.User;
            
            switch (appointment.WithType)
            {
                case AppointmentWithType.HealthCoach:
                    var hcPayload = _mapper.Map<CompletedHealthCoachAppointmentPayload>(appointment);
                    
                    await _eventBus.Publish(new AppointmentIntegrationEvent(
                        payload: hcPayload,
                        patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                        eventDate: DateTime.UtcNow), cancellationToken);
                    break;
                case AppointmentWithType.Provider:
                case AppointmentWithType.HealthCoachAndProvider:
                    var medicalPayload = _mapper.Map<CompletedMedicalAppointmentPayload>(appointment);
                    
                    await _eventBus.Publish(new AppointmentIntegrationEvent(
                        payload: medicalPayload,
                        patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                        eventDate: DateTime.UtcNow), cancellationToken);
                    break;
            }

         
        }
    }
}

