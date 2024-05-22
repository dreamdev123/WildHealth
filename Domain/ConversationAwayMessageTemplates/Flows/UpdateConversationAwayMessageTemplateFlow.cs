using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Domain.ConversationAwayMessageTemplates.Flows;

public record UpdateConversationAwayMessageTemplateFlow(ConversationAwayMessageTemplate Template, string NewTitle,  string NewBody) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        Template.Title = NewTitle;
        Template.Body = NewBody;

        return Template.Updated();
    }
}