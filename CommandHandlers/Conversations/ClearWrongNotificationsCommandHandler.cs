using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Enums;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class ClearWrongNotificationsCommandHandler :  MessagingBaseService, IRequestHandler<ClearWrongNotificationsCommand>
{
    private readonly IConversationParticipantMessageReadIndexService _conversationParticipantMessageReadIndexService;
    private readonly IConversationsService _conversationsService;
    private readonly IEmployeeService _employeeService;
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly INotificationService _notificationService;

    public ClearWrongNotificationsCommandHandler(
        IConversationParticipantMessageReadIndexService conversationParticipantMessageReadIndexService,
        IConversationsService conversationsService,
        IEmployeeService employeeService,
        ISettingsManager settingsManager,
        ITwilioWebClient twilioWebClient,
        INotificationService notificationService
        ) : base(settingsManager)
    {
        _conversationsService = conversationsService;
        _employeeService = employeeService;
        _conversationParticipantMessageReadIndexService = conversationParticipantMessageReadIndexService;
        _twilioWebClient = twilioWebClient;
        _notificationService = notificationService;
    }
    
    public async Task Handle(ClearWrongNotificationsCommand request, CancellationToken cancellationToken)
    {
        var employees = await _employeeService.GetAllActiveEmployeesAsync();

        foreach (var employee in employees)
        {
            // get all conversations where employee is
            var employeeConversations = await _conversationsService.GetConversationsByEmployeeAsync(employee.GetId(), true);

            if (!employeeConversations.Any())
            {
                continue;
            }

            bool hasUnreadMessages = false;
                        
            foreach (var employeeConversation in employeeConversations)
            {
                // Initialize the web client
                var credentials = await GetMessagingCredentialsAsync(employeeConversation.PracticeId);

                _twilioWebClient.Initialize(credentials);
                
                // Get read index by employee
                var readIndexes = await _conversationParticipantMessageReadIndexService.GetByConversationAndParticipantAsync(
                    employeeConversation.VendorExternalId, employee.User.UniversalId.ToString());

                // get last message from twilio in conversation
                var messagesFromConversation = await _twilioWebClient.GetMessagesAsync(employeeConversation.VendorExternalId, MessagesOrderType.desc.ToString(), 1);

                var lastMessage = messagesFromConversation.Messages.FirstOrDefault();

                var index = lastMessage?.Index ?? 0;

                // if last message index equal in our database last read index that means all messages was read. If not we skip that employee
                if (readIndexes is null || index != readIndexes.LastReadIndex)
                {
                    hasUnreadMessages = true;
                    break;
                }
            }

            if (!hasUnreadMessages)
            {
                // remove all notifications connected with messaging
                try
                {
                    var notifications = await _notificationService.GetNotificationsAsync(new int?[] { employee.UserId });

                    if (!notifications.Any())
                    {
                        continue;
                    }
              
                    var notificationsToDelete =
                        notifications
                            .Where(x => x.Type.Equals(NotificationType.NewMessage) || x.Type.Equals(NotificationType.UnreadMessagesCount))
                            .ToArray();

                    foreach (var notification in notificationsToDelete)
                    {
                        await _notificationService.DeleteUserNotificationAsync(
                            employee.UserId,
                            notification.GetId());
                    }
                }
                catch
                {
                    // skip removing notifications
                }
            }
        }
    }
}