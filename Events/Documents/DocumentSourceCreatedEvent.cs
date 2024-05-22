using MediatR;
using WildHealth.Domain.Entities.Documents;

namespace WildHealth.Application.Events.Documents;

public class DocumentSourceCreatedEvent : INotification
{
    public DocumentSource DocumentSource { get; }

    public DocumentSourceCreatedEvent(DocumentSource documentSource)
    {
        DocumentSource = documentSource;
    }
}