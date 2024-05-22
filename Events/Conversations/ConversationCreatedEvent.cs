using WildHealth.Domain.Entities.Conversations;
using WildHealth.Shared.Enums;
using MediatR;

namespace WildHealth.Application.Events.Conversations
{
    public class ConversationCreatedEvent : INotification
    {
        public Conversation Conversation { get; }

        public UserType CreatedBy { get; }

        public ConversationCreatedEvent(Conversation conversation, UserType createdBy)
        {
            Conversation = conversation;
            CreatedBy = createdBy;
        }
    }
}
