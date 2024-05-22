using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.Documents.Flows;
using WildHealth.Application.Commands.Documents;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Documents;
using WildHealth.Domain.Entities.Documents;

namespace WildHealth.Application.CommandHandlers.Documents;

public class UpdateDocumentSourceCommandHandler : IRequestHandler<UpdateDocumentSourceCommand, DocumentSource>
{
    private readonly IDocumentSourcesService _documentSourcesService;
    private readonly MaterializeFlow _materialize;
    
    public UpdateDocumentSourceCommandHandler(IDocumentSourcesService documentSourcesService, MaterializeFlow materialize)
    {
        _documentSourcesService = documentSourcesService;
        _materialize = materialize;
    }

    public async Task<DocumentSource> Handle(UpdateDocumentSourceCommand command, CancellationToken cancellationToken)
    {
        var documentSource = await _documentSourcesService.GetByIdAsync(command.DocumentSourceId);

        await new UpdateDocumentSourceTagsFlow(documentSource, command.Tags).Materialize(_materialize);
        
        await new UpdateDocumentSourcePersonasFlow(documentSource, command.PersonaIds).Materialize(_materialize);
        
        await new UpdateDocumentSourceFlow(documentSource, command.Name).Materialize(_materialize);

        return documentSource;
    }
}