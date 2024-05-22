using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.CommandHandlers.Notes.Flows;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Goals;
using WildHealth.Application.Utils.NotesParser;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Notes;

public class ParseGoalsFromNoteCommandHandler : IRequestHandler<ParseGoalsFromNoteCommand>
{
    private readonly MaterializeFlow _materializeFlow;
    private readonly IGoalsService _goalsService;
    private readonly INotesParser _notesParser;

    public ParseGoalsFromNoteCommandHandler(
        MaterializeFlow materializeFlow, 
        IGoalsService goalsService, 
        INotesParser notesParser)
    {
        _materializeFlow = materializeFlow;
        _goalsService = goalsService;
        _notesParser = notesParser;
    }

    public async Task Handle(ParseGoalsFromNoteCommand command, CancellationToken cancellationToken)
    {
        var note = command.Note;

        var currentGoals = await _goalsService.GetCurrentAsync(note.PatientId);

        var noteGoals = _notesParser.ParseGoals(note);

        var flow = new ParseGoalsFromNoteFlow(
            patient: note.Patient,
            currentGoals: currentGoals,
            noteGoals: noteGoals,
            DateTime.UtcNow
        );
        
        await flow.Materialize(_materializeFlow);
    }
}