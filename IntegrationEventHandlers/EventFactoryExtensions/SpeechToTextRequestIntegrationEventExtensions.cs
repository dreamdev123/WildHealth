using WildHealth.Application.Events.Documents;
using WildHealth.Domain.Enums.Documents;
using WildHealth.IntegrationEvents.SpeechToTextRequests.Payloads;

namespace WildHealth.Application.IntegrationEventHandlers.EventFactoryExtensions;

public static class SpeechToTextRequestIntegrationEventExtensions
{
    public static TranscriptionStatusChangedEvent ToTranscriptionStatusChangedEvent(
        this TranscriptionStatusChangedPayload source)
    {
        
        // See all available statuses here: https://eastus.dev.cognitive.microsoft.com/docs/services/speech-to-text-api-v3-1/operations/WebHooks_Create
        var newStatus = source.Status switch
        {
            "TranscriptionCreation" => AutomatedDocumentSourceItemStatus.Acknowledged,
            "TranscriptionProcessing" => AutomatedDocumentSourceItemStatus.DocumentGenerationStarted,
            "TranscriptionCompletion" => AutomatedDocumentSourceItemStatus.DocumentGenerationSucceeded,
            _ => AutomatedDocumentSourceItemStatus.None
        };
        
        return new(status: newStatus, transcriptionId: source.TranscriptionId, displayName: source.DisplayName);
    }
}