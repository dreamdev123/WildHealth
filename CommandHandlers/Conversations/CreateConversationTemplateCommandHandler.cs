using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.CommandHandlers.Conversations.Flows;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Entities.Conversations;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class CreateConversationTemplateCommandHandler : IRequestHandler<CreateConversationTemplateCommand, ConversationTemplate>
{
    private readonly MaterializeFlow _materializeFlow;

    public CreateConversationTemplateCommandHandler(MaterializeFlow materializeFlow)
    {
        _materializeFlow = materializeFlow;
    }

    public async Task<ConversationTemplate> Handle(CreateConversationTemplateCommand command, CancellationToken cancellationToken)
    {
        var flow = new CreateConversationTemplateFlow(
            Name: command.Name,
            Description: command.Description,
            Text: command.Text,
            Order: command.Order,
            Type: command.Type
        );

        var result = await flow.Materialize(_materializeFlow);

        return result.Select<ConversationTemplate>();
    }
}