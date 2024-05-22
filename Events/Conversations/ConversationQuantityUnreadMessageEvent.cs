using MediatR;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Events.Conversations
{
    public class ConversationQuantityUnreadMessageEvent : INotification
    {
        public User User { get; }
        public int QuantityOfUnreadMessage { get; }
        public string PracticeName { get; }

        public ConversationQuantityUnreadMessageEvent(
            User user,
            int quantityOfUnreadMessage,
            string practiceName
        )
        {
            QuantityOfUnreadMessage = quantityOfUnreadMessage;
            User = user;
            PracticeName = practiceName;
        }
    }
}
