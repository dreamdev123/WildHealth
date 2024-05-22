using System;
using WildHealth.Application.Events.PatientRecommendations;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Recommendations;

namespace WildHealth.Application.CommandHandlers.Recommendations.Flows;

public class VerifyPatientRecommendationFlow : IMaterialisableFlow
{
    private readonly PatientRecommendation _patientRecommendation;

    public VerifyPatientRecommendationFlow(PatientRecommendation patientRecommendation)
    {
        _patientRecommendation = patientRecommendation;
    }

    public MaterialisableFlowResult Execute()
    {
        _patientRecommendation.Verified = true;

        return _patientRecommendation.Updated() + new PatientRecommendationVerifiedEvent(new [] { _patientRecommendation });
    }
}
