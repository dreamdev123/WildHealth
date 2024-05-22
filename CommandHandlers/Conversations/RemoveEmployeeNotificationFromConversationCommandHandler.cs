using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Entities.Notifications;
using WildHealth.Domain.Enums;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class RemoveEmployeeNotificationFromConversationCommandHandler : IRequestHandler<RemoveEmployeeNotificationFromConversationCommand>
{
    private readonly INotificationService _notificationService;

    public RemoveEmployeeNotificationFromConversationCommandHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
      public async Task Handle(RemoveEmployeeNotificationFromConversationCommand command, CancellationToken cancellationToken)
      {
          try
          {
              var notifications = await _notificationService.GetNotificationsAsync(new int?[] { command.UserId });

              if (!notifications.Any())
              {
                  return;
              }
              
              var notificationsToDelete =
                  notifications
                      .Where(x => x.Type.Equals(NotificationType.NewMessage))
                      .Where(x=> IsNotificationFromConversation(command.ConversationId, x))
                      .ToArray();

              foreach (var newMessageNotification in notificationsToDelete)
              {
                  await _notificationService.DeleteUserNotificationAsync(command.UserId,
                          newMessageNotification.GetId());
              }

              var unreadMessageNotification =
                  notifications.FirstOrDefault(x => x.Type.Equals(NotificationType.UnreadMessagesCount));

              if (unreadMessageNotification is null)
              {
                  return;
              }
              
              var countUnreadMessages = GetCountUnreadMessagesFromNotification(unreadMessageNotification);

              if (countUnreadMessages is null)
              {
                  return;
              }
              
              if (countUnreadMessages - notificationsToDelete.Length <= 0)
              {
                  await _notificationService.DeleteUserNotificationAsync(command.UserId, unreadMessageNotification.GetId());
              }
          }
          catch
          {
              // skip removing notifications
          }
      }
      
      private int? GetCountUnreadMessagesFromNotification(Notification notification)
      {
          var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(notification.LinkData);

          int result = 0;
          bool successResult = false;
            
          if (values is not null && values.ContainsKey("messageCount"))
          {
              successResult = int.TryParse(values["messageCount"], out result);
          }

          return successResult ? result : null;
      }
        
      private bool IsNotificationFromConversation(int conversationId, Notification notification)
      {
          var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(notification.LinkData);

          if (values is not null && values.ContainsKey("conversationId"))
          {
              if (values["conversationId"].Equals(conversationId.ToString()))
              {
                  return true;
              }
          }

          return false;
      }
}