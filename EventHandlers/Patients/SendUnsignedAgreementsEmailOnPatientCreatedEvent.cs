using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Agreements;
using WildHealth.Application.Events.Patients;
using MediatR;
using WildHealth.Application.Services.Patients;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.EventHandlers.Patients
{
    public class SendUnsignedAgreementsEmailOnPatientCreatedEvent : INotificationHandler<PatientCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly IPatientsService _patientsService;

        public SendUnsignedAgreementsEmailOnPatientCreatedEvent(
            IMediator mediator,
            IPatientsService patientsService)
        {
            _mediator = mediator;
            _patientsService = patientsService;
        }

        public async Task Handle(PatientCreatedEvent notification, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(notification.PatientId, PatientSpecifications.PatientUserSpecification);
            
            if (!patient.IsAllAgreementsConfirmed())
            {
                await _mediator.Send(new SendUnsignedAgreementsEmailCommand(patient), cancellationToken);
            }
        }
    }
}