using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Domain.ConversationAwayMessageTemplates.Flows;

public record DeleteConversationAwayMessageTemplateFlow(ConversationAwayMessageTemplate Template) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        return Template.Deleted();
    }
}