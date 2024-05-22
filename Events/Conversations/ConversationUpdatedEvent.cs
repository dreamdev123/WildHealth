using WildHealth.Domain.Entities.Conversations;
using WildHealth.Shared.Enums;
using MediatR;

namespace WildHealth.Application.Events.Conversations
{
    public class ConversationUpdatedEvent : INotification
    {
        public Conversation Conversation { get; }

        public ConversationUpdatedEvent(Conversation conversation)
        {
            Conversation = conversation;
        }
    }
}