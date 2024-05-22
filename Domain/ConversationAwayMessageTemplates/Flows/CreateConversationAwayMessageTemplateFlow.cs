using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Domain.ConversationAwayMessageTemplates.Flows;

public record CreateConversationAwayMessageTemplateFlow(string Title,  string Body) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        return new ConversationAwayMessageTemplate
        {
            Title = Title,
            Body = Body
        }.Added();
    }
}