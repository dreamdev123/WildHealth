using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Common.Models.Recommendations;
using WildHealth.Domain.Entities.Recommendations;
using WildHealth.Domain.Enums.Metrics;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Recommendations;

public class RecommendationsService : IRecommendationsService
{
    private readonly IGeneralRepository<Recommendation> _recommendationsRepository;
    private readonly IGeneralRepository<PatientRecommendation> _patientRecommendationsRepository;
    private readonly IGeneralRepository<RecommendationTrigger> _recommendationTriggersRepository;
    private readonly IGeneralRepository<RecommendationTriggerComponent> _recommendationTriggerComponentsRepository;
    private readonly IGeneralRepository<RecommendationTag> _recommendationTagsRepository;
    private readonly IGeneralRepository<RecommendationVerificationMethod> _recommendationVerificationMethodRepository;
    private readonly int _firstRecommendationTriggerId = 2736;  // This is where the "new" recommendations that are based on triggers start

    public RecommendationsService(
        IGeneralRepository<Recommendation> recommendationsRepository,
        IGeneralRepository<PatientRecommendation> patientRecommendationsRepository,
        IGeneralRepository<RecommendationTrigger> recommendationTriggersRepository,
        IGeneralRepository<RecommendationTriggerComponent> recommendationTriggerComponentsRepository,
        IGeneralRepository<RecommendationTag> recommendationTagsRepository,
        IGeneralRepository<RecommendationVerificationMethod> recommendationVerificationMethodRepository)
    {
        _recommendationsRepository = recommendationsRepository;
        _patientRecommendationsRepository = patientRecommendationsRepository;
        _recommendationTriggersRepository = recommendationTriggersRepository;
        _recommendationTriggerComponentsRepository = recommendationTriggerComponentsRepository;
        _recommendationTagsRepository = recommendationTagsRepository;
        _recommendationVerificationMethodRepository = recommendationVerificationMethodRepository;
    }

    /// <summary>
    /// Returns recommendation by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Recommendation> GetByIdAsync(int id)
    {
        var result = await _recommendationsRepository
            .All()
            .ById(id)
            .Include(o => o.RecommendationTriggers)
                .ThenInclude(o => o.RecommendationTriggerComponents)
                .ThenInclude(o => o.ClassificationTypeOption)
            .Include(o => o.VerificationMethods)
            .Include(o => o.Tags)
            .FindAsync();
        
        return result;
    }
    
    public async Task<Recommendation[]> GetActiveAsync()
    {
        var result = await _recommendationsRepository
            .All()
            .Where(o => !o.IsDeleted)
            .Where(o => o.Id >= _firstRecommendationTriggerId)
            .Include(o => o.RecommendationTriggers.Where(x => !x.IsDeleted))
                .ThenInclude(o => o.RecommendationTriggerComponents.Where(x => !x.IsDeleted))
                .ThenInclude(o => o.ClassificationTypeOption)
            .Include(o => o.Tags)
            .Include(o => o.VerificationMethods)
            .ToArrayAsync();
        
        return result;
    }

    public async Task<PatientReportRecommendationModel[]> GetPatientRecommendationsAsync(int patientId)
    {
        var patientRecommendations = await _patientRecommendationsRepository
            .All()
            .RelatedToPatient(patientId)
            .Include(x => x.PatientRecommendationTriggers)
            .ThenInclude(x => x.Trigger)
            .ThenInclude(x => x.Recommendation)
            .AsNoTracking()
            .ToArrayAsync();
        
        return patientRecommendations
            // .SelectMany(x => x.PatientRecommendationTriggers)
            .Select(x =>
            {
                var patientRecommendationTriggers = x.PatientRecommendationTriggers;
                var firstPrTrigger = patientRecommendationTriggers.First();
                var firstTrigger = firstPrTrigger.Trigger;

                return new PatientReportRecommendationModel
                {
                    Id = firstTrigger.GetId(),
                    RecommendationId = firstTrigger.RecommendationId,
                    RecommendationType = firstTrigger.Type,
                    Recommendation = new RecommendationShortModel
                    {
                        Id = firstTrigger.RecommendationId,
                        Content = firstPrTrigger.PatientRecommendation.Content,
                        Order = firstTrigger.Recommendation.Order,
                    }
                };
            })
            .ToArray();
    }

    public async Task<Recommendation[]> GetRecommendationsByMetricSourceAsync(MetricSource[] sources)
    {
        var result = await _recommendationsRepository
            .All()
            .Include(o => o.RecommendationTriggers.Where(x => !x.IsDeleted))
                .ThenInclude(o => o.RecommendationTriggerComponents.Where(x => !x.IsDeleted))
                .ThenInclude(o => o.Metric)
            .Include(o => o.RecommendationTriggers.Where(x => !x.IsDeleted))
                .ThenInclude(o => o.RecommendationTriggerComponents.Where(x => !x.IsDeleted))
                .ThenInclude(o => o.ClassificationTypeOption)
            .Include(o => o.VerificationMethods)
            .Where(o => !o.IsDeleted && o.RecommendationTriggers.Any(x =>
                x.RecommendationTriggerComponents.Any(c => sources.Contains(c.Metric.Source))))
            .ToArrayAsync();

        return result;
    }

    /// <summary>
    /// Creates recommendation
    /// </summary>
    /// <param name="recommendation"></param>
    /// <returns></returns>
    public async Task<Recommendation> CreateAsync(Recommendation recommendation)
    {
        await _recommendationsRepository.AddAsync(recommendation);

        await _recommendationsRepository.SaveAsync();

        return recommendation;
    }

    /// <summary>
    /// Updates recommendation
    /// </summary>
    /// <param name="recommendation"></param>
    /// <returns></returns>
    public async Task<Recommendation> UpdateAsync(Recommendation recommendation)
    {
        _recommendationsRepository.Edit(recommendation);

        await _recommendationsRepository.SaveAsync();
        
        return recommendation;
    }

    public async Task DeleteAsync(int id)
    {
        var recommendation = await GetByIdAsync(id);

        recommendation.IsDeleted = true;
        
        foreach (var trigger in recommendation.RecommendationTriggers)
        {
            trigger.IsDeleted = true;

            foreach (var component in trigger.RecommendationTriggerComponents)
            {
                component.IsDeleted = true;
            }
        }
        
        await UpdateAsync(recommendation);
    }

    public async Task<RecommendationTrigger[]> GetTriggersByRecommendationIdAsync(int recommendationId)
    {
        var triggers = await _recommendationTriggersRepository
            .All()
            .Where(o => !o.IsDeleted && o.RecommendationId == recommendationId)
            .Include(o => o.RecommendationTriggerComponents)
            .ToArrayAsync();
        
        return triggers;
    }

    public async Task<RecommendationTrigger> GetTriggerAsync(int recommendationTriggerId)
    {
        var trigger = await _recommendationTriggersRepository
            .All()
            .ById(recommendationTriggerId)
            .Include(o => o.RecommendationTriggerComponents)
            .FindAsync();

        return trigger;
    }

    public async Task<RecommendationTrigger> CreateTriggerAsync(RecommendationTrigger trigger)
    {
        await _recommendationTriggersRepository.AddAsync(trigger);

        await _recommendationTriggersRepository.SaveAsync();

        return trigger;
    }

    public async Task<RecommendationTrigger> UpdateTriggerAsync(RecommendationTrigger trigger)
    {
        _recommendationTriggersRepository.Edit(trigger);

        await _recommendationTriggersRepository.SaveAsync();
        
        return trigger;
    }

    public async Task DeleteTriggerAsync(int triggerId)
    {
        var trigger = await GetTriggerAsync(triggerId);

        trigger.IsDeleted = true;
        
        foreach (var component in trigger.RecommendationTriggerComponents)
        {
            component.IsDeleted = true;
        }

        await UpdateTriggerAsync(trigger);
    }
    
    public async Task<RecommendationTriggerComponent[]> GetTriggerComponentsByRecommendationTriggerIdAsync(int recommendationTriggerId)
    {
        var components = await _recommendationTriggerComponentsRepository
            .All()
            .Where(o => o.RecommendationTriggerId == recommendationTriggerId && !o.IsDeleted)
            .ToArrayAsync();

        return components;
    }

    public async Task DeleteTriggerComponentAsync(RecommendationTriggerComponent triggerComponent)
    {
        triggerComponent.IsDeleted = true;

        _recommendationTriggerComponentsRepository.Edit(triggerComponent);

        await _recommendationTriggerComponentsRepository.SaveAsync();
    }

    public async Task<RecommendationTag> AddTagAsync(RecommendationTag tag)
    {
        await _recommendationTagsRepository.AddAsync(tag);

        await _recommendationTagsRepository.SaveAsync();

        return tag;
    }
    
    public async Task RemoveTagAsync(RecommendationTag tag)
    {
        _recommendationTagsRepository.Delete(tag);

        await _recommendationTagsRepository.SaveAsync();

    }

    public async Task<RecommendationVerificationMethod> AddVerificationMethodAsync(RecommendationVerificationMethod verificationMethod)
    {
        await _recommendationVerificationMethodRepository.AddAsync(verificationMethod);

        await _recommendationVerificationMethodRepository.SaveAsync();

        return verificationMethod;
    }
    
    public async Task RemoveVerificationMethodAsync(RecommendationVerificationMethod verificationMethod)
    {
        _recommendationVerificationMethodRepository.Delete(verificationMethod);

        await _recommendationVerificationMethodRepository.SaveAsync();

    }

    public async Task<Recommendation[]> GetRecentlyAddedOrUpdatedRecommendationsAsync(DateTime from)
    {
        var result = await _recommendationsRepository
            .All()
            .Include(o => o.RecommendationTriggers.Where(x => !x.IsDeleted))
                .ThenInclude(o => o.RecommendationTriggerComponents.Where(x => !x.IsDeleted))
                .ThenInclude(o => o.Metric)
                .ThenInclude(o => o.PatientMetrics)
            .Include(o => o.RecommendationTriggers.Where(x => !x.IsDeleted))
                .ThenInclude(o => o.RecommendationTriggerComponents.Where(x => !x.IsDeleted))
                .ThenInclude(o => o.ClassificationTypeOption)
            .Include(o => o.VerificationMethods)
            .Where(o => !o.IsDeleted && o.RecommendationTriggers.Any(x =>
                x.CreatedAt >= from ||
                x.ModifiedAt >= from ||
                x.RecommendationTriggerComponents.Any(c => c.CreatedAt >= from || c.ModifiedAt >= from)))
            .ToArrayAsync();
        
        return result;
    }
}