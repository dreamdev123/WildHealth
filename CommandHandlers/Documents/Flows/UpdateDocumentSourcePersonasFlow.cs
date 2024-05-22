using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Documents;

namespace WildHealth.Application.CommandHandlers.Documents.Flows;

public record UpdateDocumentSourcePersonasFlow(DocumentSource DocumentSource, int[] PersonaIds) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var personasToRemove = DocumentSource.DocumentSourcePersonas.Where(dsp => !PersonaIds.Contains(dsp.PersonaId));
        
        var personasToAdd = PersonaIds.Where(p => DocumentSource.DocumentSourcePersonas.All(o => o.PersonaId != p));

        return new MaterialisableFlowResult(personasToRemove.Select(dsp => dsp.Deleted()))
               + personasToAdd.Select(id => new DocumentSourcePersona { DocumentSource = DocumentSource, PersonaId = id }.Added());
    }
}