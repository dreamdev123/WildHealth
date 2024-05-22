using OneOf.Monads;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.PatientJourney;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.Domain.PatientJourney.Flows;

public record UndoPatientJourneyTaskFlow(Option<PatientJourneyTask> PatientJourneyTask, PatientJourneyTree JourneyTree) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (!PatientJourneyTask.HasValue())
            return MaterialisableFlowResult.Empty;

        var entity = PatientJourneyTask.Value();
        if (!JourneyTree.CanUndo(entity.JourneyTaskId))
            throw new DomainException("Can't undo auto completed task");
        
        entity.Status = PatientJourneyTaskStatus.Active;

        return entity.Updated();
    }
}