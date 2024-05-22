using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class GetAllMessagesFromTwilioCommandHandler : MessagingBaseService, IRequestHandler<GetAllMessagesFromConversationCommand, ConversationMessagesModel>
    {
        private readonly int ALL_THE_MESSAGES = 1000;
        private readonly IConversationsService _conversationService;
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly ILogger _logger;

        public GetAllMessagesFromTwilioCommandHandler(
            IConversationsService conversationService,
            ISettingsManager settingsManager,
            ITwilioWebClient twilioWebClient,
            ILogger<GetAllMessagesFromTwilioCommandHandler> logger
            ) : base(settingsManager)
        {
            _conversationService = conversationService;
            _twilioWebClient = twilioWebClient;
            _logger = logger;
        }

        /// <summary>
        /// This handler return all messages from conversation by request
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConversationMessagesModel> Handle(GetAllMessagesFromConversationCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Getting all messages from conversation with id {command.VendorExternalId} has been started.");

            var conversation = await _conversationService.GetByExternalVendorIdAsync(command.VendorExternalId);

            var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

            _twilioWebClient.Initialize(credentials);

            var result = await _twilioWebClient.GetMessagesAsync(command.VendorExternalId, MessagesOrderType.asc.ToString(), ALL_THE_MESSAGES);

            _logger.LogInformation($"Finished getting all messages from conversation with vendorExternalId: {command.VendorExternalId}");

            return result!;
        }
    }
}
