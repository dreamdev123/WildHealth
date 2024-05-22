using System.Collections.Generic;
using System.Linq;
using WildHealth.Domain.Entities.PatientJourney;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Enums.PatientJourney;

namespace WildHealth.Application.Domain.PatientJourney;

public static class JourneyTaskExtensions
{
    public static int[] GetIds(this IEnumerable<JourneyTask> source) =>
        source.Select(x => x.GetId()).ToArray();
    
    public static PatientJourneyTask? LookupPatientTask(this JourneyTask source, List<PatientJourneyTask> allTasks) =>
        allTasks.FirstOrDefault(x => x.JourneyTaskId == source.GetId());

    public static AutomaticCompletionPrerequisite ToJourneyTaskCompletionPrerequisite(this AppointmentWithType source) =>
        source switch
        {
            AppointmentWithType.HealthCoach => AutomaticCompletionPrerequisite.HcVisitCompleted,
            AppointmentWithType.Provider or AppointmentWithType.HealthCoachAndProvider => AutomaticCompletionPrerequisite.PhysicianVisitCompleted,
            _ => AutomaticCompletionPrerequisite.None
        };

    public static AutomaticCompletionPrerequisite ToJourneyTaskCompletionPrerequisite(this OrderType source) =>
        source switch
        {
            OrderType.Dna => AutomaticCompletionPrerequisite.DnaResulted,
            OrderType.Lab => AutomaticCompletionPrerequisite.LabsResulted,
            _ => AutomaticCompletionPrerequisite.None
        };
}