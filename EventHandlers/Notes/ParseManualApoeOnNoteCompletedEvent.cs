using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Events.Notes;
using MediatR;

namespace WildHealth.Application.EventHandlers.Notes;

public class ParseManualApoeOnNoteCompletedEvent : INotificationHandler<NoteCompletedEvent>
{
    private readonly IMediator _mediator;

    public ParseManualApoeOnNoteCompletedEvent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(NoteCompletedEvent notification, CancellationToken cancellationToken)
    {
        var command = new ParseManualApoeFromNoteCommand(notification.Note);

        await _mediator.Send(command, cancellationToken);
    }
}