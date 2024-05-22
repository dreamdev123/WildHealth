using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Services.Links;
using WildHealth.Application.Services.Notifications;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;

namespace WildHealth.Application.EventHandlers.Conversations
{
    class SendEmailNotificationOnUnreadMessageConversationEventHandler : INotificationHandler<ConversationEmailUnreadNotificationEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IMediator _mediator;
        private readonly ILinkShortenService _linkShortenService;
        private readonly ILogger<SendEmailNotificationOnUnreadMessageConversationEventHandler> _logger;
        public SendEmailNotificationOnUnreadMessageConversationEventHandler(INotificationService notificationService, 
            IMediator mediator, ILinkShortenService linkShortenService,
            ILogger<SendEmailNotificationOnUnreadMessageConversationEventHandler> logger)
        {
            _notificationService = notificationService;
            _linkShortenService = linkShortenService;
            _mediator = mediator;
            _logger = logger;
        }
        public async Task Handle(ConversationEmailUnreadNotificationEvent notification, CancellationToken cancellationToken)
        {
            if (notification.Patient?.User is null) return;
            
            var users = new[] { notification.Patient?.User };

            await _notificationService.CreateNotificationAsync(new NewMessageOnConversationEmailNotification(
                users: users, 
                count: notification.UnreadMessageCount, 
                practiceName: notification.Practice?.Name,
                conversationType: notification.ConversationType,
                lastMessageSentDate: notification.LastMessageSentDate,
                lastMessageSentDateFormatted: notification.LastMessageSentDateFormatted,
                messageLocationText: notification.MessageLocationText
            ));
            
            // added SMS notification
            await _mediator.Send(new SendSMSNotificationForConversationReminderCommand(
                notification.Patient!, 
                notification.Practice!, 
                notification.UnreadMessageCount,
                lastMessageSentDateFormatted: notification.LastMessageSentDateFormatted,
                messageLocationText: notification.MessageLocationText,
                await GetTinyUrl(notification.Patient?.IsMobileAppInstalled() is false ? WebUrls.Clarity.MessagingURL : WebUrls.Clarity.MessagingSMS)
            ));
        }

        private async Task<string> GetTinyUrl(string messagingSMS)
        {
            try
            {
                var tags = new[] {"SMS"};
                var shortenLink = await _linkShortenService.ShortenAsync(messagingSMS, tags);
                
                return shortenLink;
            }
            catch (Exception e)
            {
                _logger.LogWarning($"The URL shortening failed: {e.ToString()}.  Using the long link instead.");
            }

            return messagingSMS;
        }
    }
}
