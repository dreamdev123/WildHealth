using Microsoft.Extensions.Logging;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class CreateConversationMessageUnreadNotificationCommandHandler : IRequestHandler<
        CreateConversationMessageUnreadNotificationCommand, ConversationMessageUnreadNotification>
    {
        private readonly IConversationMessageUnreadNotificationService _conversationMessageUnreadNotificationService;
        private readonly ILogger _logger;

        public CreateConversationMessageUnreadNotificationCommandHandler(
            IConversationMessageUnreadNotificationService conversationMessageUnreadNotificationService,
            ILogger<CreateConversationMessageUnreadNotificationCommandHandler> logger)
        {
            _conversationMessageUnreadNotificationService = conversationMessageUnreadNotificationService;
            _logger = logger;
        }

        public async Task<ConversationMessageUnreadNotification> Handle(CreateConversationMessageUnreadNotificationCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Creating ConversationMessageUnreadNotification for user with [UserId] {command.UserId} has been started.");

            var conversationMessageUnreadNotification = await _conversationMessageUnreadNotificationService
                .CreateAsync(
                    new ConversationMessageUnreadNotification(
                        command.UserId,
                        command.ConversationId,
                        command.SentAt,
                        command.LastReadMessageIndex,
                        command.UnreadMessageCount,
                        command.ConversationVendorExternalIdentity,
                        command.ParticipantVendorExternalIdentity
                    ));

            _logger.LogInformation($"Creating ConversationMessageUnreadNotification for user with [UserId] {command.UserId} has been finished.");

            return conversationMessageUnreadNotification;
        }
    }
}