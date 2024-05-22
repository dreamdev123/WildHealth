using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Notes;
using WildHealth.Application.Commands.HealthSummaries;
using MediatR;

namespace WildHealth.Application.EventHandlers.Notes;

public class UpdateHealthSummaryOnNoteCompletedEvent : INotificationHandler<NoteCompletedEvent>
{
    private readonly IMediator _mediator;

    public UpdateHealthSummaryOnNoteCompletedEvent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(NoteCompletedEvent notification, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ParseNoteToHealthSummaryCommand(notification.Note), cancellationToken);
    }
}