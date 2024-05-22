using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class UpdateConversationFavoritesCommandHandler : MessagingBaseService, IRequestHandler<UpdateConversationFavoritesCommand, Conversation>
    {
        private readonly ILogger _logger;
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly IMediator _mediator;
        private readonly IConversationsService _conversationsService;

        public UpdateConversationFavoritesCommandHandler(
            ILogger<UpdateConversationFavoritesCommandHandler> logger,
            ITwilioWebClient twilioWebClient,
            ISettingsManager settingsManager,
            IMediator mediator,
            IConversationsService conversationsService) : base(settingsManager)
        {
            _logger = logger;
            _twilioWebClient = twilioWebClient;
            _mediator = mediator;
            _conversationsService = conversationsService;
        }


        public async Task<Conversation> Handle(UpdateConversationFavoritesCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Updating Conversation with [ConversationId] {command.ConversationId} has been started.");

            var conversation = await _conversationsService.GetByIdAsync(command.ConversationId);

            var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

            _twilioWebClient.Initialize(credentials);

            var twilioConversation = await _twilioWebClient.GetConversationAsync(id: conversation.VendorExternalId);
            
            var attributes = twilioConversation.GetAttributes();

            if (command.IsAdd) {
                attributes.FavoriteEmployeeIds.Add(command.EmployeeId);
            } else {
                attributes.FavoriteEmployeeIds.Remove(command.EmployeeId);
            }

            await _twilioWebClient.UpdateConversationAsync(new ConversationModel
            {
                Sid = conversation.VendorExternalId,
                Attributes = JsonConvert.SerializeObject(attributes)
            });

            _logger.LogInformation($"Updating Conversation with [ConversationId] {command.ConversationId} has been finished.");

            return conversation;
        }
    }
}
