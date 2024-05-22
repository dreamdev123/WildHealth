using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.CommandHandlers.Conversations.Flows;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Conversations;
using WildHealth.Domain.Entities.Conversations;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class UpdateConversationTemplateCommandHandler : IRequestHandler<UpdateConversationTemplateCommand, ConversationTemplate>
{
    private readonly IConversationTemplatesService _conversationTemplatesService;
    private readonly MaterializeFlow _materializeFlow;

    public UpdateConversationTemplateCommandHandler(
        IConversationTemplatesService conversationTemplatesService, 
        MaterializeFlow materializeFlow)
    {
        _conversationTemplatesService = conversationTemplatesService;
        _materializeFlow = materializeFlow;
    }

    public async Task<ConversationTemplate> Handle(UpdateConversationTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await _conversationTemplatesService.GetAsync(command.Id);
        
        var flow = new UpdateConversationTemplateFlow(
            Template: template,
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