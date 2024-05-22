using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Agreements;
using WildHealth.Application.Events.Patients;
using MediatR;

namespace WildHealth.Application.EventHandlers.Patients
{
    public class SendUnsignedAgreementsEmailPatientMigratedFromHintEvent : INotificationHandler<PatientMigratedFromIntegrationSystemEvent>
    {
        private readonly IMediator _mediator;

        public SendUnsignedAgreementsEmailPatientMigratedFromHintEvent(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        public async Task Handle(PatientMigratedFromIntegrationSystemEvent notification, CancellationToken cancellationToken)
        {
            var patient = notification.Patient;
            if (!patient.IsAllAgreementsConfirmed())
            {
                await _mediator.Send(new SendUnsignedAgreementsEmailCommand(patient), cancellationToken);
            }
        }
    }
}