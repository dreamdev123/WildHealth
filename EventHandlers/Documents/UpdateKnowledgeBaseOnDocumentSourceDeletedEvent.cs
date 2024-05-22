using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Events.Documents;
using WildHealth.Jenny.Clients.Models;
using WildHealth.Jenny.Clients.WebClients;

namespace WildHealth.Application.EventHandlers.Documents;

public class UpdateKnowledgeBaseOnDocumentSourceDeletedEvent : INotificationHandler<DocumentSourceDeletedEvent>
{
    private readonly IJennyKnowledgeBaseWebClient _jennyKnowledgeBaseWebClient;
    
    public UpdateKnowledgeBaseOnDocumentSourceDeletedEvent(IJennyKnowledgeBaseWebClient jennyKnowledgeBaseWebClient)
    {
        _jennyKnowledgeBaseWebClient = jennyKnowledgeBaseWebClient;
    }

    public async Task Handle(DocumentSourceDeletedEvent notification, CancellationToken cancellationToken)
    {
        await _jennyKnowledgeBaseWebClient.DeleteResource(new DeleteKbResourceRequestModel()
        {
            ResourceIds = notification.ChunkUniversalIds.Select(uid => uid.ToString()).ToArray()
        });
    }
}