using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;

namespace WildHealth.Application.EventHandlers.Conversations
{
    public class SendEmailNotificationOnConversationReceivedMessage : INotificationHandler<ConversationQuantityUnreadMessageEvent>
    {
        private readonly INotificationService _notificationService;

        public SendEmailNotificationOnConversationReceivedMessage(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(ConversationQuantityUnreadMessageEvent notification, CancellationToken cancellationToken)
        {
            var users =  new [] { notification.User };

            await _notificationService.CreateNotificationAsync(new UnreadMessagesCountNotification(users, notification.QuantityOfUnreadMessage, notification.PracticeName));
        }
    }
}
