using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Medications;
using WildHealth.Application.Services.Supplements;
using WildHealth.Application.Utils.NotesParser;
using WildHealth.Application.CommandHandlers.Notes.Flows;
using WildHealth.Application.Functional.Flow;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Notes;

public class ParseMedicationsAndSupplementsFromNoteCommandHandler : IRequestHandler<ParseMedicationsAndSupplementsFromNoteCommand>
{
    private readonly INotesParser _notesParser;
    private readonly MaterializeFlow _materializeFlow;
    private readonly IPatientsSupplementsService _supplementsService;
    private readonly IPatientMedicationsService _medicationsService;

    public ParseMedicationsAndSupplementsFromNoteCommandHandler(
        INotesParser notesParser, 
        MaterializeFlow materializeFlow, 
        IPatientsSupplementsService supplementsService, 
        IPatientMedicationsService medicationsService)
    {
        _notesParser = notesParser;
        _materializeFlow = materializeFlow;
        _supplementsService = supplementsService;
        _medicationsService = medicationsService;
    }

    public async Task Handle(ParseMedicationsAndSupplementsFromNoteCommand command, CancellationToken cancellationToken)
    {
        var note = command.Note;

        var noteMedications = _notesParser.ParseMedications(note.Content);
        var noteSupplements = _notesParser.ParseSupplements(note.Content);
        var existingMedications = await _medicationsService.GetAsync(note.PatientId);
        var existingSupplements = await _supplementsService.GetAsync(note.PatientId);

        var flow = new ParseMedicationsAndSupplementsFromNoteFlow(
            note: note,
            noteMedications: noteMedications,
            noteSupplements: noteSupplements,
            existingMedications: existingMedications.ToArray(),
            existingSupplements: existingSupplements.ToArray()
        );
        
        await flow.Materialize(_materializeFlow);
    }
}