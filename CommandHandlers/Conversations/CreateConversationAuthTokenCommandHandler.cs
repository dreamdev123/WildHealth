using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Services.Messaging.Auth;
using WildHealth.Domain.Entities.Users;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class CreateConversationAuthTokenCommandHandler : IRequestHandler<CreateConversationAuthTokenCommand, User>
    {
        private readonly IUsersService _usersService;
        private readonly IMessagingAuthService _messagingAuthService;
        private readonly ILogger _logger;

        public CreateConversationAuthTokenCommandHandler(
            IUsersService usersService,
            IMessagingAuthService messagingAuthService,
            ILogger<CreateConversationAuthTokenCommandHandler> logger)
        {
            _usersService = usersService;
            _messagingAuthService = messagingAuthService;
            _logger = logger;
        }

        public async Task<User> Handle(CreateConversationAuthTokenCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Creating conversation auth token for user with id: {command.UserId} has been started.");
            
            var user = await _usersService.GetAsync(command.UserId);

            var authTokenModel = await _messagingAuthService.CreateConversationAuthToken(user.PracticeId, user.MessagingIdentity());

            user.ConversationIdentity = user.MessagingIdentity();
            user.ConversationAuthToken = authTokenModel.Token;

            await _usersService.UpdateAsync(user);

            _logger.LogInformation($"Creating conversation auth token for user with id: {command.UserId} has been finished.");

            return user;
        }
    }
}
