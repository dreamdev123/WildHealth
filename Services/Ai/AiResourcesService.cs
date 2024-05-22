using System;
using System.Threading.Tasks;
using WildHealth.Application.Extensions.Query;
using WildHealth.Application.Services.Ai.Flows;
using WildHealth.Common.Models.Ai;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Entities.Recommendations;
using WildHealth.Domain.Exceptions;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Ai;

public class AiResourcesService : IAiResourceService
{
    private readonly IGeneralRepository<PatientRecommendation> _patientRecommendationsRepository;
    private readonly IGeneralRepository<DocumentChunk> _documentChunksRepository;

    public AiResourcesService(
    IGeneralRepository<PatientRecommendation> patientRecommendationsRepository, 
    IGeneralRepository<DocumentChunk> documentChunksRepository)
    {
        _patientRecommendationsRepository = patientRecommendationsRepository;
        _documentChunksRepository = documentChunksRepository;
    }

    public async Task<AiResourceModel> GetAiResourceAsync(Guid universalId, string resourceType)
    {
        return resourceType switch
        {
            AiConstants.ResourceTypes.PatientRecommendation => await GetPatientRecommendation(universalId),
            AiConstants.ResourceTypes.GeneralKnowledgeBaseChunk 
                or AiConstants.ResourceTypes.QuestionnaireAnswer
                or AiConstants.ResourceTypes.PatientDocumentChunk
                or AiConstants.ResourceTypes.PatientMeetingTranscriptChunk => await GetDocumentChunk(universalId),
            _ => throw new DomainException($"Ai resource type {resourceType} is not supported")
        };
    }
    
    #region private

    private async Task<AiResourceModel> GetPatientRecommendation(Guid universalId)
    {
        var result = await _patientRecommendationsRepository
            .All()
            .Query(source => new GetRecommendationResourceQueryFlow(source, universalId))
            .FindAsync();

        return result;
    }

    private async Task<AiResourceModel> GetDocumentChunk(Guid universalId)
    {
         var result = await _documentChunksRepository
                    .All()
                    .Query(source => new GetDocumentChunkResourceQueryFlow(source, universalId))
                    .FindAsync();
        
                return result;
    }
    
    #endregion
}