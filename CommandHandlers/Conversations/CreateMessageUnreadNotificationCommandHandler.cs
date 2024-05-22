using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.CommandHandlers.Conversations
{


    /// <summary>
    /// This handler will create a clarity notification to user when mark message as unread
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>s
    public class CreateMessageUnreadNotificationCommandHandler:IRequestHandler<CreateMessageUnreadNotificationCommand, Unit>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger _logger;

        public CreateMessageUnreadNotificationCommandHandler(
            INotificationService notificationService,
            ILogger<CreateScheduledMessageCommandHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }
        
        public async Task<Unit> Handle(CreateMessageUnreadNotificationCommand request, CancellationToken cancelationToken)
        {
            _logger.LogInformation($"Starting handler creating notification for unread message por [userId]: {request.User.Id}");
            try
            {
                var users = new List<User> { request.User };
                var model = new UnreadMessagesCountClarityOnlyNotification()
                {
                    Users = users,
                    Subject = "Marked Messages as Unread",
                    Text = "Marked Messages as Unread",
                    CreatedAt = new DateTime(),
                    Type = request.Type,
                    LinkDataItems = new Dictionary<string, string>
                    {
                        { "conversationId", request.ConversationId.ToString() },
                        { "conversationType", request.ConversationType}
                    }
                };
                await _notificationService.CreateNotificationAsync(model);

            }
            catch (Exception err)
            {
                _logger.LogWarning($"Error on Notification with error : {err.Message}");
                return Unit.Value;
            }
            
            return Unit.Value;
        }
    }
    
}