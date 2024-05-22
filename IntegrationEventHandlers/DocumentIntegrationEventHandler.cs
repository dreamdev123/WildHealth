using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Documents;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Documents;
using WildHealth.IntegrationEvents.Documents.Payloads;
using WildHealth.Jenny.Clients.WebClients;
using MediatR;

namespace WildHealth.Application.IntegrationEventHandlers;

public class DocumentIntegrationEventHandler : IEventHandler<DocumentIntegrationEvent>
{
    private readonly IJennyKnowledgeBaseWebClient _jennyKnowledgeBaseWebClient;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public DocumentIntegrationEventHandler(
        IJennyKnowledgeBaseWebClient jennyKnowledgeBaseWebClient, 
        IMediator mediator, 
        ILogger<DocumentIntegrationEventHandler> logger)
    {
        _jennyKnowledgeBaseWebClient = jennyKnowledgeBaseWebClient;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DocumentIntegrationEvent @event)
    {
        _logger.LogInformation($"Event received {@event}");

        switch (@event.PayloadType)
        {
            case nameof(DocumentChunkingCompletedPayload):
                await HandleDocumentChunkingCompleted(@event.DeserializePayload<DocumentChunkingCompletedPayload>());
                break;
           
            default:
                throw new ArgumentOutOfRangeException($"Unknown dorothy form payload type of {@event.PayloadFullType}");
        }
    }
    
    #region private

    private async Task HandleDocumentChunkingCompleted(DocumentChunkingCompletedPayload payload)
    {
        var chunksResponse = await _jennyKnowledgeBaseWebClient.GetChunks(payload.RequestId);
        
        await _mediator.Send( ProcessDocumentChunksCommand.ByRequestId(
            requestId: payload.RequestId,
            chunks: chunksResponse
        ));
    }

    #endregion
}