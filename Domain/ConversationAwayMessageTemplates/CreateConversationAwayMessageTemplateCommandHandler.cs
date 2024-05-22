using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Domain.ConversationAwayMessageTemplates.Commands;
using WildHealth.Application.Domain.ConversationAwayMessageTemplates.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Domain.ConversationAwayMessageTemplates;

public class CreateConversationAwayMessageTemplateCommandHandler : IRequestHandler<CreateConversationAwayMessageTemplateCommand, int>
{
    private readonly MaterializeFlow _materializer;

    public CreateConversationAwayMessageTemplateCommandHandler(MaterializeFlow materializer)
    {
        _materializer = materializer;
    }

    public async Task<int> Handle(CreateConversationAwayMessageTemplateCommand request, CancellationToken cancellationToken)
    {
        var created = await new CreateConversationAwayMessageTemplateFlow(request.Title, request.Body).Materialize(_materializer);

        return created.Select<ConversationAwayMessageTemplate>().GetId();
    }
}