using System.Linq;
using System.Collections.Generic;
using WildHealth.Application.Events.PatientRecommendations;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Recommendations;
using WildHealth.Domain.Enums.Recommendations;

namespace WildHealth.Application.CommandHandlers.Recommendations.Flows;

public class ProcessRecommendationFlow : IMaterialisableFlow
{
    private readonly int _patientId;
    private readonly Recommendation _recommendation;
    private readonly PatientRecommendation? _existingPatientRecommendation;
    private readonly PatientMetric[] _patientMetrics;
    private readonly IDictionary<string, string> _data;
    
    public ProcessRecommendationFlow(
        int patientId, 
        Recommendation recommendation, 
        PatientRecommendation? existingPatientRecommendation,
        PatientMetric[] patientMetrics,
        IDictionary<string, string> data)
    {
        _patientId = patientId;
        _recommendation = recommendation;
        _existingPatientRecommendation = existingPatientRecommendation;
        _patientMetrics = patientMetrics;
        _data = data;
    }
    
    public MaterialisableFlowResult Execute()
    {
        var triggers = _recommendation.RecommendationTriggers.Where(trigger => trigger.LogicalOperator == Shared.Enums.LogicalOperator.And
            ? trigger.RecommendationTriggerComponents.All(SatisfiesTrigger)
            : trigger.RecommendationTriggerComponents.Any(SatisfiesTrigger)).ToArray();

        var triggered = triggers.Any();
        
        // Check if patient has this recommendation but should be removed
        if (!triggered && _existingPatientRecommendation is not null)
        {
            // Remove recommendation
            return _existingPatientRecommendation.Deleted() 
            + new PatientRecommendationRemovedEvent(
                UserUniversalId: _existingPatientRecommendation.Patient.User.UserId(), 
                PatientRecommendationUniversalId: _existingPatientRecommendation.UniversalId);
        }

        // Check if patient doesn't have this recommendation but should be added
        if (triggered && _existingPatientRecommendation is null)
        {
            // Add recommendation
            var content = _recommendation.InsertData(data: _data);

            if (_recommendation.HasAllelesCounter())
            {
                var triggeredMetrics = _recommendation.RecommendationTriggers
                    .SelectMany(t => t.RecommendationTriggerComponents)
                    .Where(SatisfiesTrigger)
                    .Select(rtc => rtc.MetricId);
                var triggeredPatientMetrics = _patientMetrics.Where(pm => triggeredMetrics.Contains(pm.MetricId));
                content = _recommendation.GetAllelesCountLanguage(triggeredPatientMetrics.Select(pm => pm.Value).ToArray());
            }

            var isVerified = _recommendation.VerificationMethods.Any(o => o.VerificationMethod == VerificationMethod.Automatic);

            var patientRecommendation = new PatientRecommendation(
                recommendationId: _recommendation.GetId(), 
                patientId: _patientId, 
                content: content,
                verified: isVerified,
                triggers: triggers 
            );

            MaterialisableFlowResult result = patientRecommendation.Added();

            if (isVerified)
            {
                result += new PatientRecommendationVerifiedEvent(new [] { patientRecommendation });
            }
        
            return result;
        } 
        
        return MaterialisableFlowResult.Empty;
    }
    
    private bool SatisfiesTrigger(RecommendationTriggerComponent component) =>
        _patientMetrics.Where(o => o.MetricId == component.MetricId).MaxBy(x => x.CreatedAt)?.ClassificationTypeOptionId ==
        component.ClassificationTypeOptionId;
}

