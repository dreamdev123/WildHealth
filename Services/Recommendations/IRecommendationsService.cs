using System;
using System.Threading.Tasks;
using WildHealth.Common.Models.Recommendations;
using WildHealth.Common.Models.Reports;
using WildHealth.Domain.Entities.Recommendations;
using WildHealth.Domain.Enums.Metrics;

namespace WildHealth.Application.Services.Recommendations;

public interface IRecommendationsService
{
    /// <summary>
    /// Returns recommendation by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Recommendation> GetByIdAsync(int id);
    
    /// <summary>
    /// Returns active recommendations
    /// </summary>
    /// <returns></returns>
    Task<Recommendation[]> GetActiveAsync();
    
    /// <summary>
    /// Returns patient recommendations
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    Task<PatientReportRecommendationModel[]> GetPatientRecommendationsAsync(int patientId);

    /// <summary>
    /// Returns recommendations by metric source
    /// </summary>
    /// <param name="sources"></param>
    /// <returns></returns>
    Task<Recommendation[]> GetRecommendationsByMetricSourceAsync(MetricSource[] sources);

    /// <summary>
    /// Creates recommendation
    /// </summary>
    /// <param name="recommendation"></param>
    /// <returns></returns>
    Task<Recommendation> CreateAsync(Recommendation recommendation);

    /// <summary>
    /// Returns triggers for a recommendation
    /// </summary>
    /// <param name="recommendationId"></param>
    /// <returns></returns>
    Task<RecommendationTrigger[]> GetTriggersByRecommendationIdAsync(int recommendationId);
    
    /// <summary>
    /// Returns a trigger for a recommendation
    /// </summary>
    /// <param name="recommendationTriggerId"></param>
    /// <returns></returns>
    Task<RecommendationTrigger> GetTriggerAsync(int recommendationTriggerId);
    
    /// <summary>
    /// Creates recommendation trigger
    /// </summary>
    /// <param name="trigger"></param>
    /// <returns></returns>
    Task<RecommendationTrigger> CreateTriggerAsync(RecommendationTrigger trigger);
    
    /// <summary>
    /// Updates recommendation
    /// </summary>
    /// <param name="recommendation"></param>
    /// <returns></returns>
    Task<Recommendation> UpdateAsync(Recommendation recommendation);
    
    /// <summary>
    /// Creates recommendation trigger
    /// </summary>
    /// <param name="trigger"></param>
    /// <returns></returns>
    Task<RecommendationTrigger> UpdateTriggerAsync(RecommendationTrigger trigger);
    
    /// <summary>
    /// Soft delete recommendation trigger
    /// </summary>
    /// <param name="triggerId"></param>
    /// <returns></returns>
    Task DeleteTriggerAsync(int triggerId);

    /// <summary>
    /// Soft deletes recommendation
    /// </summary>
    /// <returns></returns>
    Task DeleteAsync(int id);

    /// <summary>
    /// Gets trigger components
    /// </summary>
    /// <param name="recommendationTriggerId"></param>
    /// <returns></returns>
    Task<RecommendationTriggerComponent[]> GetTriggerComponentsByRecommendationTriggerIdAsync(int recommendationTriggerId);
    
    /// <summary>
    /// Soft deletes trigger component
    /// </summary>
    /// <param name="triggerComponent"></param>
    /// <returns></returns>
    Task DeleteTriggerComponentAsync(RecommendationTriggerComponent triggerComponent);

    /// <summary>
    /// Adds a tag to the recommendation
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    Task<RecommendationTag> AddTagAsync(RecommendationTag tag);
    
    /// <summary>
    /// Removes a tag from the recommendation
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    Task RemoveTagAsync(RecommendationTag tag);

    /// <summary>
    /// Adds a verification method to the recommendation
    /// </summary>
    /// <param name="verificationMethod"></param>
    /// <returns></returns>
    Task<RecommendationVerificationMethod> AddVerificationMethodAsync(RecommendationVerificationMethod verificationMethod);

    /// <summary>
    /// Removes a verification method from the recommendation
    /// </summary>
    /// <param name="verificationMethod"></param>
    /// <returns></returns>
    Task RemoveVerificationMethodAsync(RecommendationVerificationMethod verificationMethod);

    Task<Recommendation[]> GetRecentlyAddedOrUpdatedRecommendationsAsync(DateTime from);
}