using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.CommandHandlers.Conversations.Flows;

public record CreateConversationTemplateFlow(
    string Name,
    string Description,
    string Text,
    int Order,
    ConversationType Type): IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var template = new ConversationTemplate()
        {
            Name = Name,
            Description = Description,
            Text = Text,
            Order = Order,
            Type = Type,
            UserType = UserType.Employee
        };

        return template.Added();
    }
}