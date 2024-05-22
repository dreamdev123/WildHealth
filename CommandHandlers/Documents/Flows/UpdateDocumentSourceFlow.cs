using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Documents;

namespace WildHealth.Application.CommandHandlers.Documents.Flows;

public record UpdateDocumentSourceFlow(DocumentSource DocumentSource, string Name) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        DocumentSource.Name = Name;
        
        return DocumentSource.Updated();
    }
}