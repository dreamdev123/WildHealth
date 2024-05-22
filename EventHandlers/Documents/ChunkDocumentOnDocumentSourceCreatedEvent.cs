using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Documents;
using WildHealth.Application.Events.Documents;
using WildHealth.Application.Services.Documents;
using WildHealth.Common.Enums;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Jenny.Clients.Models;
using WildHealth.Jenny.Clients.WebClients;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Common.Constants;
using WildHealth.Domain.Enums.Integrations;
using MediatR;
using Microsoft.Azure.Storage.Blob;
using WildHealth.Application.Utils.AzureBlobProvider;

namespace WildHealth.Application.EventHandlers.Documents;

public class ChunkDocumentOnDocumentSourceCreatedEvent : INotificationHandler<DocumentSourceCreatedEvent>
{
    private readonly IJennyKnowledgeBaseWebClient _jennyKnowledgeBaseWebClient;
    private readonly IDocumentSourcesService _documentSourcesService;
    private readonly IFeatureFlagsService _featureFlagsService;
    private readonly IMediator _mediator;    
    private readonly ILogger _logger;
    private readonly IAzureBlobProvider _azureBlobProvider;

    public ChunkDocumentOnDocumentSourceCreatedEvent(
        IJennyKnowledgeBaseWebClient jennyKnowledgeBaseWebClient,
        IDocumentSourcesService documentSourcesService,
        IFeatureFlagsService featureFlagsService,
        IMediator mediator,
        ILogger<ChunkDocumentOnDocumentSourceCreatedEvent> logger,
        IAzureBlobProvider azureBlobProvider)
    {
        _jennyKnowledgeBaseWebClient = jennyKnowledgeBaseWebClient;
        _documentSourcesService = documentSourcesService;
        _featureFlagsService = featureFlagsService;
        _mediator = mediator;
        _logger = logger;
        _azureBlobProvider = azureBlobProvider;
    }

    public async Task Handle(DocumentSourceCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Attempting chunk and store [DocumentSourceId] - {notification.DocumentSource.Name}, with [Id] = {notification.DocumentSource.GetId()}");
        
        var documentSource = await _documentSourcesService.GetByIdAsync(notification.DocumentSource.GetId());
        var flow = GetFLowType();

        var _ = flow switch
        {
            FlowType.Regular => await ExecuteRegularFlow(documentSource),
            FlowType.Asynchronous => await ExecuteAsynchronousFlow(documentSource),
            _ => throw new ArgumentException(nameof(FlowType))
        };
    }

    private async Task<bool> ExecuteRegularFlow(DocumentSource documentSource)
    {
        var chunksResponse = await _jennyKnowledgeBaseWebClient.Chunk(BuildRequest(documentSource));

        _logger.LogInformation($"Received {chunksResponse.Chunks.Length} chunks from the chunking web client");

        var command = ProcessDocumentChunksCommand.ByDocumentId(documentSource.GetId(), chunksResponse);

        await _mediator.Send(command);
        
        return true;
    }

    private async Task<bool> ExecuteAsynchronousFlow(DocumentSource documentSource)
    {
        var request = await _jennyKnowledgeBaseWebClient.ChunkAsync(BuildRequest(documentSource));

        documentSource.LinkWithIntegrationSystem(request.Id, IntegrationVendor.Jenny);

        await _documentSourcesService.UpdateAsync(documentSource);
        
        _logger.LogInformation(@"Asynchronous to chunk document with id {documentId} was sent.", documentSource.Id);
        
        return true;
    }

    private DocumentChunkRequestModel BuildRequest(DocumentSource documentSource)
    {
        var documentGenerationStrategy = documentSource.DocumentSourceType.GenerationStrategy;
        var chunkingStrategy = documentSource.DocumentSourceType.ChunkingStrategy;

        CloudBlobContainer? container = null;
        
        if (documentSource.File is not null)
        {
            container = _azureBlobProvider.GetBlobContainer(documentSource.File.ContainerName);
        }
        
        return new DocumentChunkRequestModel
        {
            DocumentGenerationStrategy = documentGenerationStrategy,
            ChunkingStrategy = chunkingStrategy,
            Url = documentSource.Url,
            FileName = documentSource.File?.Name,
            ContainerName = container?.Name
        };
    }
    
    private FlowType GetFLowType()
    {
        return _featureFlagsService.GetFeatureFlag(FeatureFlags.AsyncChunking) 
            ? FlowType.Asynchronous 
            : FlowType.Regular;
    }
}