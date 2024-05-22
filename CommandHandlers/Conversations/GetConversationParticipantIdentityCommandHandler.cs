using System;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Settings;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class GetConversationParticipantIdentityCommandHandler : MessagingBaseService, IRequestHandler<GetConversationParticipantIdentityCommand, (int, Guid, bool)>
    {
        private readonly IConversationsService _conversationService;
        private readonly ILogger _logger;

        public GetConversationParticipantIdentityCommandHandler(
            IConversationsService conversationService,
            ISettingsManager settingsManager,
            ILogger<GetAllMessagesFromTwilioCommandHandler> logger
            ) : base(settingsManager)
        {
            _conversationService = conversationService;
            _logger = logger;
        }

        /// <summary>
        /// This handler return all messages from conversation by request
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<(int, Guid, bool)> Handle(GetConversationParticipantIdentityCommand command, CancellationToken cancellationToken)
        {
            var conversation = await _conversationService.GetByIdAsync(command.ConversationId);
            return new ConversationDomain(conversation, _logger.LogInformation).GetParticipantIdentity(command.ParticipantSid);
        }
    }
}
