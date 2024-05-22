using MediatR;

namespace WildHealth.Application.Commands.Documents;

public class DeleteDocumentSourceCommand : IRequest
{
    public int DocumentSourceId { get; }

    public DeleteDocumentSourceCommand(int documentSourceId)
    {
        DocumentSourceId = documentSourceId;
    }
}