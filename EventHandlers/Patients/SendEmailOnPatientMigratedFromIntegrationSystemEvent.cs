using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Events.Patients;
using MediatR;

namespace WildHealth.Application.EventHandlers.Patients
{
    public class SendEmailOnPatientMigratedFromIntegrationSystemEvent : INotificationHandler<PatientMigratedFromIntegrationSystemEvent>
    {
        private readonly IMediator _mediator;

        public SendEmailOnPatientMigratedFromIntegrationSystemEvent(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(PatientMigratedFromIntegrationSystemEvent notification, CancellationToken cancellationToken)
        {
            if (notification.DPC)
            {
                var command = new SendMigrateDPCPatientEmailCommand(notification.Patient, notification.Subscription);
                await _mediator.Send(command, cancellationToken);
            }
            else
            {
                var command = new SendMigratePatientEmailCommand(notification.Patient, notification.Subscription);
                await _mediator.Send(command, cancellationToken);
            }
        }
    }
}