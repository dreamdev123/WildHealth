using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Events.Payments;
using MediatR;

namespace WildHealth.Application.EventHandlers.Payments
{
    public class SendEmailOnSubscriptionChangedEvent : INotificationHandler<SubscriptionChangedEvent>
    {
        private readonly IMediator _mediator;

        public SendEmailOnSubscriptionChangedEvent(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(SubscriptionChangedEvent notification, CancellationToken cancellationToken)
        {
            var command = new SendConfirmationEmailCommand(
                patient: notification.Patient,
                newSubscription: notification.NewSubscription,
                previousSubscription: notification.PreviousSubscription,
                patientAddOnIds: notification.PatientAddOnIds);

            await _mediator.Send(command, cancellationToken);
        }
    }
}
