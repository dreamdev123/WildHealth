using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Notes;
using WildHealth.Application.Commands.Notes;
using MediatR;

namespace WildHealth.Application.EventHandlers.Notes;

public class UpdateMedicationsAndSupplementsOnNoteCompletedEvent : INotificationHandler<NoteCompletedEvent>
{
    private readonly IMediator _mediator;

    public UpdateMedicationsAndSupplementsOnNoteCompletedEvent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(NoteCompletedEvent notification, CancellationToken cancellationToken)
    {
        var note = notification.Note;

        var command = new ParseMedicationsAndSupplementsFromNoteCommand(note);

        await _mediator.Send(command, cancellationToken);
    }
}