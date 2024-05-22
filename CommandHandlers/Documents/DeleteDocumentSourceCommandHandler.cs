using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.Documents.Flows;
using WildHealth.Application.Commands.Documents;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Documents;

namespace WildHealth.Application.CommandHandlers.Documents;

public class DeleteDocumentSourceCommandHandler : IRequestHandler<DeleteDocumentSourceCommand>
{
    private readonly IDocumentSourcesService _documentSourcesService;
    private readonly MaterializeFlow _materialize;

    public DeleteDocumentSourceCommandHandler(IDocumentSourcesService documentSourcesService,
        MaterializeFlow materialize)
    {
        _documentSourcesService = documentSourcesService;
        _materialize = materialize;
    }

    public async Task Handle(DeleteDocumentSourceCommand command, CancellationToken cancellationToken)
    {
        var documentSource = await _documentSourcesService.GetByIdAsync(command.DocumentSourceId);
        
        await new DeleteDocumentSourceFlow(documentSource).Materialize(_materialize);
    }
}