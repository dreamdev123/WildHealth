using MediatR;
using WildHealth.Domain.Enums.Documents;

namespace WildHealth.Application.Events.Documents;

public class TranscriptionStatusChangedEvent : INotification
{
    public AutomatedDocumentSourceItemStatus Status { get; set; }
    public string TranscriptionId { get; set; }
    
    public string DisplayName { get; set; }

    public TranscriptionStatusChangedEvent(AutomatedDocumentSourceItemStatus status, string transcriptionId, string displayName)
    {
        Status = status;
        TranscriptionId = transcriptionId;
        DisplayName = displayName;
    }
}