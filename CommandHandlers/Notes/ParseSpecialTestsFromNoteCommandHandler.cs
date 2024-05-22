using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.CommandHandlers.Notes.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Utils.NotesParser;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Utils.DateTimes;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Notes;

public class ParseSpecialTestsFromNoteCommandHandler : IRequestHandler<ParseSpecialTestsFromNoteCommand>
{
    private readonly MaterializeFlow _materializeFlow;
    private readonly IEmployeeService _employeeService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotesParser _notesParser;

    public ParseSpecialTestsFromNoteCommandHandler(
        MaterializeFlow materializeFlow, 
        IEmployeeService employeeService,
        IDateTimeProvider dateTimeProvider,
        INotesParser notesParser)
    {
        _materializeFlow = materializeFlow;
        _employeeService = employeeService;
        _dateTimeProvider = dateTimeProvider;
        _notesParser = notesParser;
    }

    public async Task Handle(ParseSpecialTestsFromNoteCommand command, CancellationToken cancellationToken)
    {
        var note = command.Note;

        var specialTests = _notesParser.ParseSpecialTests(note.Content);

        var employee = await _employeeService.GetByIdAsync(note.CompletedById!.Value);

        var now = _dateTimeProvider.UtcNow();
        
        var flow = new ParseSpecialTestsFlow(
            patient: note.Patient,
            employee: employee,
            specialTests: specialTests,
            now: now
        );
        
        await flow.Materialize(_materializeFlow);
    }
}