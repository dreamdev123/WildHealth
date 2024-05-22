using WildHealth.Domain.Entities.Notes;

namespace WildHealth.Application.CommandHandlers.Notes.Flows;

public class CancelNoteSignOffFlow
{
    private readonly Note _note;

    public CancelNoteSignOffFlow(Note note)
    {
        _note = note;
    }

    public CancelNoteSignOffFlowResult Execute()
    {
        if (_note.AssignedToId == null)
        {
            return new CancelNoteSignOffFlowResult(_note);
        }
        _note.AssignedToId = null;
        _note.AssignedAt = null;
        _note.AdditionalNote = null;

        return new CancelNoteSignOffFlowResult(_note);
    }
}

public record CancelNoteSignOffFlowResult(Note Note);