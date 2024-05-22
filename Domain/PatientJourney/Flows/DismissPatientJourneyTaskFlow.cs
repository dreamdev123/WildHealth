using OneOf.Monads;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.PatientJourney;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Extensions;
using static WildHealth.Domain.Entities.PatientJourney.PatientJourneyTaskStatus;

namespace WildHealth.Application.Domain.PatientJourney.Flows;

public record DismissPatientJourneyTaskFlow(
    int PatientId, 
    JourneyTask JourneyTask, 
    Option<PatientJourneyTask> PatientJourneyTask) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        if (PatientJourneyTask.HasValue() && IsCompleted(PatientJourneyTask.Value())) // can dismiss uncompleted tasks only
            return MaterialisableFlowResult.Empty;

        if (JourneyTask.IsDeleted())
            return MaterialisableFlowResult.Empty;

        if (JourneyTask.IsRequired)
            throw new DomainException("Only optional task can be dismissed");
        
        return PatientJourneyTask.HasValue() ? Dismiss(PatientJourneyTask.Value()) : new PatientJourneyTask
        {
            PatientId = PatientId,
            JourneyTaskId = JourneyTask.GetId(),
            Status = Dismissed
        }.Added();
    }

    private MaterialisableFlowResult Dismiss(PatientJourneyTask t)
    {
        t.Status = Dismissed;
        return t.Updated();
    }

    private bool IsCompleted(PatientJourneyTask t)
    {
        return (t.Status & PatientCompleted) == PatientCompleted ||
               (t.Status & AutoCompleted) == AutoCompleted ||
               t.Status == Dismissed;
    }
}