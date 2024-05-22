using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.IntegrationEvents.Patients.Payloads;
using WildHealth.IntegrationEvents.Patients;
using WildHealth.IntegrationEvents._Base;
using WildHealth.Infrastructure.Communication.MessageBus;
using MediatR;

namespace WildHealth.Application.EventHandlers.Patients
{
    public class SendIntegrationEventOnPatientRegisteredEvent : INotificationHandler<PatientRegisteredEvent>
    {
        private readonly IEventBus _eventBus;
        private readonly IPatientsService _patientsService;

        public SendIntegrationEventOnPatientRegisteredEvent(IPatientsService patientsService, IEventBus eventBus)
        {
            _patientsService = patientsService;
            _eventBus = eventBus;
        }

        public async Task Handle(PatientRegisteredEvent notification, CancellationToken cancellationToken)
        {
            var payload = new PatientRegisteredPayload(
                practiceId: notification.PracticeId,
                patientId: notification.PatientId,
                employeeId: notification.EmployeeId,
                linkedEmployeeId: notification.LinkedEmployeeId,
                locationId: notification.LocationId,
                paymentPriceId: notification.PaymentPriceId,
                subscriptionId: notification.SubscriptionId,
                isTrialPlan: notification.IsTrialPlan,
                employerProductKey: notification.EmployerProductKey,
                addonIds: notification.AddonIds,
                inviteCode: notification.InviteCode,
                founderId: notification.FounderId,
                leadSourceId: notification.LeadSource?.LeadSourceId,
                otherLeadSource: notification.LeadSource?.OtherLeadSource,
                podcastSource: notification.LeadSource?.PodcastSource
            );
            
            var patient = await _patientsService.GetByIdAsync(notification.PatientId);
            
            await _eventBus.Publish(new PatientIntegrationEvent(
                payload: payload,
                patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                practice: new PracticeMetadataModel(notification.PracticeId),
                eventDate: DateTime.UtcNow
            ), cancellationToken);
        }
    }
}
