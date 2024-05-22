using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.Medication;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Entities.Supplement;

namespace WildHealth.Application.CommandHandlers.Notes.Flows;

public class ParseMedicationsAndSupplementsFromNoteFlow: IMaterialisableFlow
{
    private readonly Note _note;
    private readonly NoteMedicationModel[] _noteMedications;
    private readonly NoteSupplementModel[] _noteSupplements;
    private readonly PatientMedication[] _existingMedications;
    private readonly PatientSupplement[] _existingSupplements;

    public ParseMedicationsAndSupplementsFromNoteFlow(
        Note note,
        NoteMedicationModel[] noteMedications, 
        NoteSupplementModel[] noteSupplements, 
        PatientMedication[] existingMedications, 
        PatientSupplement[] existingSupplements)
    {
        _note = note;
        _noteMedications = noteMedications;
        _noteSupplements = noteSupplements;
        _existingMedications = existingMedications;
        _existingSupplements = existingSupplements;
    }
    
    public MaterialisableFlowResult Execute()
    {
        var medicationsToDeleteIds = _noteMedications
            .Where(x => x.IsInCurrent)
            .Where(x => x.IsStopped)
            .Select(x => x.Id)
            .ToArray();
        
        var medicationsToDelete = _existingMedications
            .Where(x => medicationsToDeleteIds.Contains(x.GetId()))
            .ToArray();
            
        var supplementsToDeleteIds = _noteSupplements
            .Where(x => x.IsInCurrent)
            .Where(x => x.IsStopped)
            .Select(x => x.Id)
            .ToArray();

        var supplementsToDelete = _existingSupplements
            .Where(x => supplementsToDeleteIds.Contains(x.GetId()))
            .ToArray();
        
        var medicationsToCreate = _noteMedications
            .Where(x => !x.IsInCurrent)
            .Select(x => new PatientMedication(_note.PatientId, x.Name)
            {
                Dosage = x.Dosage,
                Instructions = x.Instructions,
                StartDate = x.StartDate
            })
            .ToArray();
            
        var supplementsToCreate = _noteSupplements
            .Where(x => !x.IsInCurrent)
            .Select(x => new PatientSupplement(_note.PatientId, x.Name)
            {
                Dosage = x.Dosage,
                Instructions = x.Instructions,
                PurchaseLink = x.PurchaseLink
            })
            .ToArray();

        return new MaterialisableFlowResult(
            medicationsToDelete.Select(x => x.Deleted())
                .Concat(supplementsToDelete.Select(x => x.Deleted())
                .Concat(medicationsToCreate.Select(x => x.Added())
                .Concat(supplementsToCreate.Select(x => x.Added()))))
        );
    }
}