using MediatR;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Events.Notes;

public class NoteSignOffRequestedEvent : INotification
{
    public Note Note { get; }
    public User AssignedTo { get; }
    
    public NoteSignOffRequestedEvent(Note note, User assignedTo)
    {
        Note = note;
        AssignedTo = assignedTo;
    }
}