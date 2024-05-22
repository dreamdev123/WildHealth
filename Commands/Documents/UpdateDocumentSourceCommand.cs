using MediatR;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Enums.Recommendations;

namespace WildHealth.Application.Commands.Documents;

public class UpdateDocumentSourceCommand : IRequest<DocumentSource>
{
    public int DocumentSourceId { get; }
    
    public string Name { get; }
    
    public int[] PersonaIds { get; }
    
    public HealthCategoryTag[]  Tags { get; }

    public UpdateDocumentSourceCommand(int documentSourceId, string name, int[] personaIds, HealthCategoryTag[] tags)
    {
        DocumentSourceId = documentSourceId;
        Name = name;
        PersonaIds = personaIds;
        Tags = tags;
    }
}