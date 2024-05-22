using MediatR;
using WildHealth.Domain.Entities.Notes;

namespace WildHealth.Application.Events.Notes;

public class NoteCompletedEvent : INotification
{
    public Note Note { get; }

    public NoteCompletedEvent(Note note)
    {
        Note = note;
    }
}