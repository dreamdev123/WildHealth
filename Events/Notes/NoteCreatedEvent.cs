using MediatR;
using WildHealth.Domain.Entities.Notes;

namespace WildHealth.Application.Events.Notes
{
    public class NoteCreatedEvent : INotification
    {
        public Note Note { get; }

        public NoteCreatedEvent(Note note)
        {
            Note = note;
        }
    }
}