using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;

namespace WildHealth.Application.CommandHandlers.Conversations.Flows;

public record UpdateConversationTemplateFlow(
    ConversationTemplate Template,
    string Name,
    string Description,
    string Text,
    int Order,
    ConversationType Type) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        Template.Name = Name;
        Template.Description = Description;
        Template.Text = Text;
        Template.Order = Order;
        Template.Type = Type;

        return Template.Updated();
    }
}