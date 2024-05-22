using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Documents;
using WildHealth.Application.Services.Documents;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Enums.Documents;
using WildHealth.Jenny.Clients.Models;
using WildHealth.Jenny.Clients.WebClients;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Exceptions;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Documents;

public class ProcessDocumentChunksCommandHandler : IRequestHandler<ProcessDocumentChunksCommand>
{
    private readonly IJennyKnowledgeBaseWebClient _jennyKnowledgeBaseWebClient;
    private readonly IDocumentSourcesService _documentSourcesService;
    private readonly ILogger _logger;

    public ProcessDocumentChunksCommandHandler(
        IJennyKnowledgeBaseWebClient jennyKnowledgeBaseWebClient, 
        IDocumentSourcesService documentSourcesService, 
        ILogger<ProcessDocumentChunksCommandHandler> logger)
    {
        _jennyKnowledgeBaseWebClient = jennyKnowledgeBaseWebClient;
        _documentSourcesService = documentSourcesService;
        _logger = logger;
    }

    public async Task Handle(ProcessDocumentChunksCommand command, CancellationToken cancellationToken)
    {
        var chunksResponse = command.Chunks;
        var documentSource = await GetDocumentAsync(command);

        if (documentSource is null)
        {
            throw new DomainException("Document source does not exist");
        }
        
        var chunkingStrategy = documentSource.DocumentSourceType.ChunkingStrategy;

        _logger.LogInformation($"Received {chunksResponse.Chunks.Length} chunks from the chunking web client");

        documentSource = await _documentSourcesService.StoreChunks(documentSource, chunksResponse.Chunks.Select((c) => new DocumentChunk(
            content: c.Content,
            chunkingStrategy: chunkingStrategy)
        {
            Tags = c.Tags.ToArray()
        }).ToArray());
            
        var userUniversalIds = documentSource.PatientDocumentSources.Any()
            ? documentSource.PatientDocumentSources.Select(o => o.Patient.User.UserId()).ToArray()
            : new string?[] { null };

        _logger.LogInformation($"The document source relates to the following users: {string.Join(", ", userUniversalIds)}");

        foreach (var userUniversalId in userUniversalIds)
        {
            _logger.LogInformation($"Storing chunks for the following [UserUniversalId] = {userUniversalId}");
            
            await _jennyKnowledgeBaseWebClient.StoreChunks(new DocumentChunkStoreRequestModel
            {
                UserUniversalId = userUniversalId,
                Chunks = documentSource.DocumentChunks.Select((c) => new DocumentChunkStoreModel
                {
                    Document = c.Content,
                    ResourceId = c.UniversalId.ToString(),
                    ResourceType = GetResourceType(documentSource.DocumentSourceType.Type),
                    Tags = c.Tags.Select(o => o.ToString()).ToArray(),
                    Personas = documentSource.DocumentSourcePersonas.Select(o => o.Persona.Name).ToArray()
                }).ToArray()
            });
        }
    }
    
    private string GetResourceType(SourceType type)
    {
        return type switch
        {
            SourceType.Blog 
                or SourceType.Podcast 
                or SourceType.ResearchArticle
                or SourceType.PodcastTranscript
                or SourceType.Other => AiConstants.ResourceTypes.GeneralKnowledgeBaseChunk,
            SourceType.MeetingTranscript => AiConstants.ResourceTypes.PatientMeetingTranscriptChunk,
            SourceType.PatientDocument => AiConstants.ResourceTypes.PatientDocumentChunk,
            _ => throw new ArgumentOutOfRangeException($"Unrecognized source type of {type}")
        };
    }

    private async Task<DocumentSource?> GetDocumentAsync(ProcessDocumentChunksCommand command)
    {
        if (command.DocumentSourceId.HasValue)
        {
            return await _documentSourcesService.GetByIdAsync(command.DocumentSourceId.Value);
        }

        return await _documentSourcesService.GetByIntegrationIdAsync(
            integrationId: command.RequestId, 
            vendor: IntegrationVendor.Jenny,
            purpose: IntegrationPurposes.DocumentSource.RequestId);
    }
}