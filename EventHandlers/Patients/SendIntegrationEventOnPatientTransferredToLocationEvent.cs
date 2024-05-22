using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Patients;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Patients;
using WildHealth.IntegrationEvents.Patients.Payloads;
using MediatR;
using WildHealth.Application.Services.Patients;

namespace WildHealth.Application.EventHandlers.Patients
{
    public class SendIntegrationEventOnPatientTransferredToLocationEvent : INotificationHandler<PatientTransferredToLocationEvent>
    {
        private readonly IEventBus _eventBus;
        private readonly IPatientsService _patientsService;

        public SendIntegrationEventOnPatientTransferredToLocationEvent(IPatientsService patientsService)
        {
            _patientsService = patientsService;
            _eventBus = EventBusProvider.Get();
        }

        public async Task Handle(PatientTransferredToLocationEvent notification, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(notification.PatientId);
            
            await _eventBus.Publish(new PatientIntegrationEvent(
                payload: new PatientLocationChangedPayload(
                    newLocationId: notification.NewLocationId,
                    oldLocationId: notification.OldLocationId,
                    planType: patient.CurrentSubscription?.GetDisplayName()),
                patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                practice: new PracticeMetadataModel(patient.User.PracticeId),
                eventDate: patient.CreatedAt), cancellationToken);
        }
    }
}