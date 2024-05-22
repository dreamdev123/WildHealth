using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Employees;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using MediatR;
using Polly;
using Twilio.Exceptions;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class GetRecentConversationMessagesCommandHandler : MessagingBaseService, IRequestHandler<GetRecentConversationMessagesCommand, ConversationMessageModel[]>
    {
        private readonly IConversationsService _conversationsService;
        private readonly ILogger _logger;
        private readonly ITwilioWebClient _twilioWebClient;

        public GetRecentConversationMessagesCommandHandler(
            ISettingsManager settingsManager,
            IConversationsService conversationsService,
            ITwilioWebClient twilioWebClient,
            ILogger<GetRecentConversationMessagesCommandHandler> logger) : base(settingsManager)
        {
            _conversationsService = conversationsService;
            _logger = logger;
            _twilioWebClient = twilioWebClient;
        }

        public async Task<ConversationMessageModel[]> Handle(GetRecentConversationMessagesCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Getting [Count] = {command.MessageCount} recent conversation messages for [ConversationSid] = {command.ConversationSid}");

            var conversation = await _conversationsService.GetByExternalVendorIdAsync(command.ConversationSid, false);

            var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

            _twilioWebClient.Initialize(credentials);
            
            var policy = Policy
                .Handle<TwilioException>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(3)
                });

            ConversationMessagesModel result = new ConversationMessagesModel();

            await policy.ExecuteAsync(
                async () =>
                {
                    result = await _twilioWebClient.GetMessagesAsync(command.ConversationSid, MessagesOrderType.desc.ToString(),
                        command.MessageCount);
                });
            
            return result.Messages.ToArray();
        }
    }
}
