using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.HealthSummaries;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Services.Inputs;
using WildHealth.Application.Utils.NotesParser;
using WildHealth.ClarityCore.WebClients.Patients;
using MediatR;
using WildHealth.Common.Models.HealthSummaries;
using WildHealth.Common.Models.Notes;

namespace WildHealth.Application.CommandHandlers.Notes;

public class ParseManualApoeFromNoteCommandHandler : IRequestHandler<ParseManualApoeFromNoteCommand>
{
    private const string DefaultSource = "Manual Blood Test";

    private readonly string[] ApoeScores = new[]
    {
        "E2/E2", "E2/E3", "E2/E4", "E3/E3", "E3/E4", "E4/E4"
    };
    
    private readonly IPatientsWebClient _patientsWebClient;
    private readonly IInputsService _inputsService;
    private readonly INotesParser _notesParser;
    private readonly IMediator _mediator;

    public ParseManualApoeFromNoteCommandHandler(
        IPatientsWebClient patientsWebClient, 
        IInputsService inputsService, 
        INotesParser notesParser, 
        IMediator mediator)
    {
        _patientsWebClient = patientsWebClient;
        _inputsService = inputsService;
        _notesParser = notesParser;
        _mediator = mediator;
    }

    public async Task Handle(ParseManualApoeFromNoteCommand command, CancellationToken cancellationToken)
    {
        var note = command.Note;
        
        var apoe = _notesParser.ParseApoe(command.Note);

        if (apoe is null)
        {
            return;
        }

        if (string.IsNullOrEmpty(apoe.Source))
        {
            apoe.Source = DefaultSource;
        }
        
        await UpdateHealthSummaryAsync(apoe, note.PatientId);

        await _inputsService.UpdateHideApoe(apoe.HideApoe, note.PatientId);
        
        if (!string.IsNullOrEmpty(apoe.Score) && ApoeScores.Contains(apoe.Score))
        {
            await _patientsWebClient.UpdatePatientManualApoeAccuracyAsync(note.PatientId, apoe.Score, apoe.Source);
        }
    }
    
    #region private

    private async Task UpdateHealthSummaryAsync(NoteApoeModel apoe, int patientId)
    {
        var healthSummaryValues = new List<HealthSummaryValueModel>();

        if (apoe.Colonoscopy is not null)
        {
            healthSummaryValues.Add(apoe.Colonoscopy);
        }

        if (apoe.Psa is not null)
        {
            healthSummaryValues.Add(apoe.Psa);
        }

        if (healthSummaryValues.Any())
        {
            var updateHealthSummaryCommand = new CreateOrUpdateHealthSummaryValuesCommand(
                values: healthSummaryValues.ToArray(), 
                patientId: patientId
            );
            
            await _mediator.Send(updateHealthSummaryCommand);
        }
    }
    
    #endregion
}