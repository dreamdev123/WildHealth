using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Settings;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Enums.Conversations;
using MediatR;
using WildHealth.Domain.Models.Conversation;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class MarkAllConversationMessagesReadCommandHandler : MessagingBaseService, IRequestHandler<MarkAllConversationMessagesReadCommand>
    {
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IConversationsService _conversationsService;

        public MarkAllConversationMessagesReadCommandHandler(
            ITwilioWebClient twilioWebClient,
            IMediator mediator,
            ISettingsManager settingsManager,
            ILogger<MarkAllConversationMessagesReadCommandHandler> logger, 
            IConversationsService conversationsService) : base(settingsManager)
        {
            _twilioWebClient = twilioWebClient;
            _mediator = mediator;
            _logger = logger;
            _conversationsService = conversationsService;
        }

        public async Task Handle(MarkAllConversationMessagesReadCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Marking all messages read for conversation with [conversationId] {command.ConversationId} has been started.");

            var conversation = await _conversationsService.GetByIdAsync(command.ConversationId);
            var conversationDomain = ConversationDomain.Create(conversation);

            var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);
            _twilioWebClient.Initialize(credentials);

            var messagesResponse = await _twilioWebClient.GetMessagesAsync(conversation.VendorExternalId, MessagesOrderType.desc.ToString(), 1 );

            if (!messagesResponse.Messages.Any())
            {
                _logger.LogInformation($"Marking all messages read for conversation with [conversationId] {command.ConversationId} has been finished. No messages to mark read");
                return;
            }

            var lastMesssageIndex = messagesResponse.Messages.First().Index;

            var allParticipants = conversationDomain.AllParticipants().Where(o => !string.IsNullOrEmpty(o.VendorExternalId)).ToArray();

            foreach (var participant in allParticipants)
            {
                await _mediator.Send(new LastReadMessageUpdateCommand(
                    conversationId: command.ConversationId,
                    conversationExternalVendorId: conversation.VendorExternalId,
                    participantExternalVendorId: participant?.VendorExternalIdentity!,
                    lastMessageReadIndex: lastMesssageIndex
                ));
            }

            _logger.LogInformation($"Marking all messages read for conversation with [conversationId] {command.ConversationId} has been finished.");
        }
    }
}
