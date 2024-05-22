using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.EventHandlers.Patients
{
    public class SendWelcomeEmailOnPatientCreatedEvent : INotificationHandler<PatientCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly IPatientsService _patientsService;
        private readonly ISubscriptionService _subscriptionService;

        public SendWelcomeEmailOnPatientCreatedEvent(
            IMediator mediator,
            IPatientsService patientsService,
            ISubscriptionService subscriptionService)
        {
            _mediator = mediator;
            _patientsService = patientsService;
            _subscriptionService = subscriptionService;
        }

        public async Task Handle(PatientCreatedEvent notification, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(notification.PatientId, PatientSpecifications.PatientUserSpecification);
            var subscription = await _subscriptionService.GetAsync(notification.SubscriptionId);
            var command = new SendWelcomeEmailCommand(
                patientId: patient.GetId(),
                subscriptionId: subscription.GetId(),
                selectedAddOnIds: notification.SelectedAddOnIds);

            await _mediator.Send(command, cancellationToken);
        }
    }
}