using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Enums.Recommendations;

namespace WildHealth.Application.CommandHandlers.Documents.Flows;

public record UpdateDocumentSourceTagsFlow(DocumentSource DocumentSource, HealthCategoryTag[] Tags) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var tagsToRemove = DocumentSource.Tags.Where(t => !Tags.Contains(t.Tag));
        
        var tagsToAdd = Tags.Where(t => DocumentSource.Tags.All(o => o.Tag != t));

        return new MaterialisableFlowResult(tagsToRemove.Select(t => t.Deleted()))
               + tagsToAdd.Select(tag => new DocumentSourceTag { DocumentSource = DocumentSource, Tag = tag }.Added());
    }
}