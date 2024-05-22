using System.Linq;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Tags;
using WildHealth.Domain.Models.Patient;

namespace WildHealth.Application.CommandHandlers.Patients.Flows;

public class GetPatientMetaDataFlow
{
    private readonly Patient _patient;
    private readonly TagRelation[] _tagRelations;

    public GetPatientMetaDataFlow(Patient patient, TagRelation[] tagRelations)
    {
        _patient = patient;
        _tagRelations = tagRelations;
    }

    public GetPatientMetaDataFlowResult Execute()
    {
        var patientDomain = PatientDomain.Create(_patient);
        var subscriptionPlanName = patientDomain.CurrentPlanName;
        var subscriptionPlanDisplayName = string.IsNullOrEmpty(subscriptionPlanName) 
            ? "No Active Membership"
            : subscriptionPlanName;
        
        var metaTags = _tagRelations.Select(x => x.Tag).ToArray();
        return new GetPatientMetaDataFlowResult(subscriptionPlanDisplayName, metaTags);
    }
}

public record GetPatientMetaDataFlowResult(string SubscriptionPlanDisplayName, Tag[] MetaTags);