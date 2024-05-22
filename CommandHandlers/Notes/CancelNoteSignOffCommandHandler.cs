using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.Notes.Flows;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Services.Notes;
using WildHealth.Domain.Entities.Notes;

namespace WildHealth.Application.CommandHandlers.Notes;

public class CancelNoteSignOffCommandHandler : IRequestHandler<CancelNoteSignOffCommand, Note>
{
    private readonly INoteService _noteService;

    public CancelNoteSignOffCommandHandler(INoteService noteService)
    {
        _noteService = noteService;
    }

    public async Task<Note> Handle(CancelNoteSignOffCommand request, CancellationToken cancellationToken)
    {
        var note = await _noteService.GetByIdAsync(request.NoteId);

        var flow = new CancelNoteSignOffFlow(note);

        var result = flow.Execute();
    
        var updatedNote = await _noteService.UpdateAsync(result.Note);
    
        return updatedNote;
    }
}