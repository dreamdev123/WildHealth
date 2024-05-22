using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.HealthSummaries;
using WildHealth.Application.Services.HealthSummaries;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.HealthSummaries;
using MediatR;
using WildHealth.Application.CommandHandlers.HealthSummaries.Flows;
using WildHealth.Application.Services.Allergies;
using WildHealth.Common.Models.Alergies;
using WildHealth.Domain.Enums.Notes;

namespace WildHealth.Application.CommandHandlers.HealthSummaries;

public class ParseNoteToHealthSummaryCommandHandler : IRequestHandler<ParseNoteToHealthSummaryCommand>
{
    private readonly IHealthSummaryService _healthSummaryService;
    private readonly IPatientAllergiesService _patientAllergiesService;

    public ParseNoteToHealthSummaryCommandHandler(
        IHealthSummaryService healthSummaryService,
        IPatientAllergiesService patientAllergiesService)
    {
        _healthSummaryService = healthSummaryService;
        _patientAllergiesService = patientAllergiesService;
    }

    public async Task Handle(ParseNoteToHealthSummaryCommand request, CancellationToken cancellationToken)
    {
        var healthSummaryMap = await _healthSummaryService.GetMapAsync();

        var patient = request.Note.Patient;
        
        var currentValues = await _healthSummaryService.GetByPatientAsync(patient.GetId());
        
        if (request.Note.Type == NoteType.Initial && !request.Note.IsOldIcc)
        {
            var keysToDelete = new[]
            {
                "PROBLEMS_LIST",
                "NOTES_DIAGNOSIS"
            };
            
            foreach (var key in keysToDelete)
            {
                await ClearOldValuesAsync(currentValues, key, patient.GetId());
            }
            
            var flowIcc = new ParseIccNoteHealthSummaryValuesFlow(request.Note);

            var result = flowIcc.Execute();
            
            await _healthSummaryService.CreateBatchAsync(result.ResultValues.ToArray());
        }
        else
        {
            var flow = new ParseNoteToHealthSummaryFlow(healthSummaryMap, request.Note);

            var (valuesToCreate, keysToDelete, allergies) = flow.Execute();
        
            await ParseAllergiesAsync(patient, allergies);

            foreach (var key in keysToDelete)
            {
                await ClearOldValuesAsync(currentValues, key, patient.GetId());
            }
        
            await _healthSummaryService.CreateBatchAsync(valuesToCreate.ToArray());
        }
    }

    private async Task ParseAllergiesAsync(Patient patient, CreatePatientAlergyModel[] allergies)
    {
        var allAllergies = await _patientAllergiesService.GetByPatientIdAsync(patient.GetId());
        
        foreach (var allergy in allAllergies)
        {
            await _patientAllergiesService.DeleteAsync(allergy.GetId());
        }
        
        foreach (var allergy in allergies)
        {
            await _patientAllergiesService.CreatePatientAllergyAsync(allergy, patient.GetId());
        }
    }
    
    private async Task ClearOldValuesAsync(HealthSummaryValue[] currentValues, string itemKey, int patientId)
    {
        var oldValues = currentValues
            .Where(x => x.Key.Contains(itemKey))
            .ToArray();

        foreach (var oldValue in oldValues)
        {
            await _healthSummaryService.DeleteAsync(patientId, oldValue.Key);
        }
    }
}
