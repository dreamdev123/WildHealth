using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Events.Conversations;
using MediatR;

namespace WildHealth.Application.EventHandlers.Conversations;

public class TicketRequestAlertAcceptedEventHandler : INotificationHandler<TicketRequestAlertAcceptedEvent>
{
    private readonly IMediator _mediator;

    public TicketRequestAlertAcceptedEventHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(TicketRequestAlertAcceptedEvent @event, CancellationToken cancellationToken)
    {
        var command = new StartSupportConversationCommand(
            patientId: @event.PatientId, 
            locationId: @event.LocationId, 
            practiceId: @event.PracticeId, 
            subject: @event.Subject 
        );
        
        await _mediator.Send(command, cancellationToken);
    }
}