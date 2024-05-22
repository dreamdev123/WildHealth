using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Commands.Patients;
using MediatR;
using WildHealth.Application.Services.Patients;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.EventHandlers.Patients
{
    public class SendNotificationOnPatientCreatedEvent : INotificationHandler<PatientCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly IPatientsService _patientsService;

        public SendNotificationOnPatientCreatedEvent(IMediator mediator, IPatientsService patientsService)
        {
            _mediator = mediator;
            _patientsService = patientsService;
        }

        public async Task Handle(PatientCreatedEvent notification, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(notification.PatientId, PatientSpecifications.PatientWithAggregationInputs);

            var command = new SendNewEnrollmentNotificationCommand(
                practiceId: patient.User.PracticeId,
                locationId: patient.LocationId,
                patientId: notification.PatientId,
                subscriptionId: notification.SubscriptionId);
            
            await _mediator.Send(command, cancellationToken);
        }
    }
}
