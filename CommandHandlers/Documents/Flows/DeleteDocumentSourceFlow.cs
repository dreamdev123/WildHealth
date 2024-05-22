using System.Linq;
using WildHealth.Application.Events.Documents;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Documents;

namespace WildHealth.Application.CommandHandlers.Documents.Flows;

public record DeleteDocumentSourceFlow(DocumentSource DocumentSource) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        return DocumentSource.Deleted() 
               + new DocumentSourceDeletedEvent(
                   DocumentSourceId: DocumentSource.GetId(), 
                   ChunkUniversalIds: DocumentSource.DocumentChunks.Select(c => c.UniversalId).ToArray());
    }
}