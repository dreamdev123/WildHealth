using System;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Twilio.Clients.WebClient;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Settings;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class SendEmailNotificationForConversationReminderCommandHandler : MessagingBaseService, IRequestHandler<SendEmailNotificationForConversationReminderCommand>
    {
        private readonly IMediator _mediator;
        private readonly IEmployeeService _employeeService;
        private readonly IConversationsService _conversationService;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly ILogger _logger;

        public SendEmailNotificationForConversationReminderCommandHandler(
            IMediator mediator,
            IEmployeeService employeeService,
            IConversationsService conversationService,
            IFeatureFlagsService featureFlagsService,
            ITwilioWebClient twilioWebClient,
            ILogger<SendEmailNotificationForConversationReminderCommandHandler> logger,
            ISettingsManager settingsManager) : base(settingsManager)
        {
            _mediator = mediator;
            _employeeService = employeeService;
            _conversationService = conversationService;
            _featureFlagsService = featureFlagsService;
            _twilioWebClient = twilioWebClient;
            _logger = logger;
        }

        public async Task Handle(SendEmailNotificationForConversationReminderCommand command, CancellationToken cancellationToken)
        {
            if (!_featureFlagsService.GetFeatureFlag(FeatureFlags.ConversationsBackgroundJobs))
            {
                return;
            }

            var employees = await _employeeService.GetAllActiveEmployeesAsync();
            
            foreach (var employee in employees)
            {
                try
                {
                    await CalculateUnreadMessagesForEmployeeAsync(employee);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Calculating unread messages for employee with [Id]: {employee.GetId()} failed with [Error] {e.ToString()}");
                }
            }
        }
        
        #region private

        private async Task CalculateUnreadMessagesForEmployeeAsync(Employee employee)
        {
            var quantityOfUnreadMessages = 0;

            var credentials = await GetMessagingCredentialsAsync(employee.User.PracticeId);
            
            var conversations = (await _conversationService.GetConversationsByEmployeeAsync(employee.GetId(), true)).Where(o => !String.IsNullOrEmpty(o.VendorExternalId));

            foreach(var conversation in conversations)
            {
                if (!conversation.EmployeeParticipants.First(x => x.EmployeeId == employee.Id).IsActive)
                {
                    continue;
                }
                
                _twilioWebClient.Initialize(credentials);

                var messagesFromConversation = await _twilioWebClient.GetMessagesAsync(conversation.VendorExternalId, MessagesOrderType.desc.ToString(), 1);

                var lastMessage = messagesFromConversation.Messages.FirstOrDefault();
                
                if (lastMessage is null)
                {
                    continue;
                }
                
                var lastMessageIndex = lastMessage.Index;

                var participantResources = await _twilioWebClient.GetConversationParticipantResourcesAsync(conversation.VendorExternalId);

                var participantVendorId = GetParticipantVendorId(employee, conversation);
                
                var employeeParticipant = participantResources.Participants.FirstOrDefault(x => !string.IsNullOrEmpty(participantVendorId) && x.Sid == participantVendorId);

                if (employeeParticipant is null)
                {
                    continue;
                }

                var lastReadMessageIndex = employeeParticipant.LastReadMessageIndex ?? 0;

                if (lastReadMessageIndex == lastMessageIndex)
                {
                    continue;
                }

                var unreadMessagesCount = lastMessageIndex - lastReadMessageIndex;

                if (unreadMessagesCount > 0)
                {
                    quantityOfUnreadMessages += unreadMessagesCount;
                }
            }
                             
            _logger.LogInformation($"Employee with id {employee.GetId()} have {quantityOfUnreadMessages} unread messages in Clarity.");

            if (quantityOfUnreadMessages > 0)
            {
                await _mediator.Publish(new ConversationQuantityUnreadMessageEvent(
                    user: employee.User, 
                    quantityOfUnreadMessage: quantityOfUnreadMessages, 
                    practiceName: employee.User.Practice?.Name!
                ));
            }
        }
        
        /// <summary>
        /// Returns employee participant vendor id in corresponding conversation
        /// </summary>
        /// <param name="employee"></param>
        /// <param name="conversation"></param>
        /// <returns></returns>
        private string? GetParticipantVendorId(Employee employee, Conversation conversation)
        {
            var participant = conversation
                .EmployeeParticipants
                .FirstOrDefault(x => x.EmployeeId == employee.GetId());

            return participant?.GetCurrentVendorExternalId();
        }
        
        #endregion
    }
}
