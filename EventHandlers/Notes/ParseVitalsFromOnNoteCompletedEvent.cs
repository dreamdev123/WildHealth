using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Events.Notes;
using MediatR;

namespace WildHealth.Application.EventHandlers.Notes;

public class ParseVitalsFromOnNoteCompletedEvent: INotificationHandler<NoteCompletedEvent>
{
    private readonly IMediator _mediator;

    public ParseVitalsFromOnNoteCompletedEvent(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task Handle(NoteCompletedEvent notification, CancellationToken cancellationToken)
    {
        var command = new ParseVitalsFromNoteCommand(notification.Note);

        await _mediator.Send(command, cancellationToken);
    }
}