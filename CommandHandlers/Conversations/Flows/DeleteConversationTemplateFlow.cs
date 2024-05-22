using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.CommandHandlers.Conversations.Flows;

public record DeleteConversationTemplateFlow(ConversationTemplate Template) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        return Template.Deleted();
    }
}