using MediatR;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Application.Services.Messaging.Conversations;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Settings;
using WildHealth.Shared.Exceptions;
using WildHealth.Twilio.Clients.Models.ConversationParticipants;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class AddEmployeeParticipantToConversationCommandHandler : MessagingBaseService, IRequestHandler<AddEmployeeParticipantToConversationCommand, Conversation>
    {
        private readonly IMessagingConversationService _messagingConversationService;
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly IMediator _mediator;
        private readonly ILogger<AddEmployeeParticipantToConversationCommandHandler> _logger;
        private readonly IConversationsService _conversationsService;
        private readonly IEmployeeService _employeeService;

        public AddEmployeeParticipantToConversationCommandHandler(
            IMessagingConversationService messagingConversationService,
            ISettingsManager settingsManager,
            ITwilioWebClient twilioWebClient,
            IEmployeeService employeeService,
            IMediator mediator,
            ILogger<AddEmployeeParticipantToConversationCommandHandler> logger, 
            IConversationsService conversationsService):base(settingsManager)
        {
            _messagingConversationService = messagingConversationService;
            _twilioWebClient = twilioWebClient;
            _logger = logger;
            _conversationsService = conversationsService;
            _mediator = mediator;
            _employeeService = employeeService;
        }

        public async Task<Conversation> Handle(AddEmployeeParticipantToConversationCommand command, CancellationToken cancellationToken)
        {
            var employeeId = command.EmployeeId;
            var conversationId = command.ConversationId;
            var conversation = await _conversationsService.GetByIdAsync(conversationId);
            var employee = await _employeeService.GetByIdAsync(employeeId);
            var isActive = command.IsActive ?? ShouldEmployeeBeActiveByDefault(employee);

            var existingParticipant = conversation.EmployeeParticipants.FirstOrDefault(x => x.EmployeeId == employeeId);
            var conversationDomain = ConversationDomain.Create(conversation);

            if (existingParticipant is not null && !existingParticipant.IsDeleted())
            {
                if (existingParticipant.IsActive)
                {
                    throw new AppException(HttpStatusCode.BadRequest, $"The employee {employee.User.FirstName} {employee.User.LastName} already added to conversation.");
                }

                if (isActive)
                {
                    existingParticipant.Activate();
                }
                
                await _conversationsService.UpdateConversationAsync(conversation);

                return conversation;
            }

            ConversationParticipantEmployee participant;

            if (existingParticipant is not null && existingParticipant.IsDeleted())
            {
                existingParticipant.RemoveDeletedStatus();

                existingParticipant.SetDelegatedBy(command.DelegatedBy);
                
                existingParticipant.SetActive(isActive);

                participant = existingParticipant;
            }
            else
            {
                participant = new ConversationParticipantEmployee(
                    employee: employee,
                    delegatedBy: command.DelegatedBy,
                    isActive: isActive
                );
                
                await _conversationsService.AddParticipantAsync(conversation, participant);
            }

            var vendorConversation = await _messagingConversationService.CreateConversationParticipantAsync(
                practiceId: employee.User.PracticeId,
                conversation: conversation,
                messagingIdentity: employee.User.MessagingIdentity(),
                name: $"{employee.User.FirstName} {employee.User.LastName}"
            );

            participant.AddVendorExternalId(new ConversationParticipantEmployeeIntegration(
                participant,
                IntegrationVendor.Twilio,
                IntegrationPurposes.Patient.ExternalId, 
                vendorConversation.Sid));
            
            participant.SetVendorExternalId(vendorConversation.Sid);
            participant.SetVendorExternalIdentity(vendorConversation.Identity);

            await _conversationsService.UpdateConversationAsync(conversation);

            var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

            _twilioWebClient.Initialize(credentials);

            var messagesFromConversation = await _twilioWebClient.GetMessagesAsync(conversation.VendorExternalId, MessagesOrderType.desc.ToString(), 1);

            var lastMessage = messagesFromConversation?.Messages?.LastOrDefault();

            if (lastMessage != null)
            {
                try
                {
                    await _twilioWebClient.UpdateConversationParticipantAsync(new UpdateConversationParticipantModel(
                        conversationSid: conversation.VendorExternalId,
                        lastReadMessageIndex: lastMessage.Index,
                        sid: vendorConversation.Sid
                    ));
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        $"Error while updating lastReadMessageIndex. conversationId:{conversationId}, employeeId:{employeeId}"
                        + $"errorMessage: {e.Message}");
                }
            }

            try
            {
                await _mediator.Send(new PublishConversationParticipantAddedEventCommand(
                    conversationVendorExternalId: conversation.VendorExternalId,
                    conversationId: conversation.GetId(),
                    subject: conversation.Subject,
                    state: conversation.State,
                    participantVendorExternalId: participant.VendorExternalId,
                    employeeUniversalId: employee.User.UserId()));
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }

            // If this employee is currently forwarding to another employee, we want to go ahead and add the forwarding employee as well
            var settings = await _mediator.Send(new GetOrCreateConversationsSettingsCommand(command.EmployeeId), cancellationToken);

            var settingsDomain = new ConversationSettingsDomain(settings);
            if (settingsDomain.IsForwardingMessages)
            {
                if (!conversationDomain.HasEmployeeParticipant(settings.ForwardEmployeeId))
                {
                    var addForwardedEmployeeCommand = new AddEmployeeParticipantToConversationCommand(
                        conversationId: command.ConversationId,
                        employeeId: settings.ForwardEmployeeId,
                        delegatedBy: command.EmployeeId,
                        isActive: isActive
                    );
                    
                    await _mediator.Send(addForwardedEmployeeCommand, cancellationToken);
                }
            }
            

            return conversation;
        }

        private bool ShouldEmployeeBeActiveByDefault(Employee employee)
        {
            return employee.RoleId == Roles.CoachId;
        }
    }
}
