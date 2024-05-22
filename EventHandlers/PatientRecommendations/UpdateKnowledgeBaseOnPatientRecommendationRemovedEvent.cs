using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Events.PatientRecommendations;
using WildHealth.Application.Services.Recommendations;
using WildHealth.Jenny.Clients.Models;
using WildHealth.Jenny.Clients.WebClients;

namespace WildHealth.Application.EventHandlers.PatientRecommendations;

public class UpdateKnowledgeBaseOnPatientRecommendationRemovedEvent : INotificationHandler<PatientRecommendationRemovedEvent>
{
    private readonly IJennyKnowledgeBaseWebClient _jennyKnowledgeBaseWebClient;
    
    public UpdateKnowledgeBaseOnPatientRecommendationRemovedEvent(
        IJennyKnowledgeBaseWebClient jennyKnowledgeBaseWebClient)
    {
        _jennyKnowledgeBaseWebClient = jennyKnowledgeBaseWebClient;
    }
    
    public async Task Handle(PatientRecommendationRemovedEvent notification, CancellationToken cancellationToken)
    {
        await _jennyKnowledgeBaseWebClient.DeleteResource(new DeleteKbResourceRequestModel
        {
            UserUniversalId = notification.UserUniversalId,
            ResourceIds = new [] { notification.PatientRecommendationUniversalId.ToString() }
        });
    }
}