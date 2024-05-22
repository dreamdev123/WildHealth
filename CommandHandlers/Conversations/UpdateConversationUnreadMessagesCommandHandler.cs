using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Settings;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Application.Services.Messaging.Base;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class UpdateConversationUnreadMessagesCommandHandler : MessagingBaseService, IRequestHandler<UpdateConversationUnreadMessagesCommand>
{
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IConversationsService _conversationsService;
    
    public UpdateConversationUnreadMessagesCommandHandler(
        ITwilioWebClient twilioWebClient,
        ILogger<UpdateConversationUnreadMessagesCommandHandler> logger, 
        IMediator mediator,
        IConversationsService conversationsService,
        ISettingsManager settingsManager) : base(settingsManager)
    {
        _twilioWebClient = twilioWebClient;
        _logger = logger;
        _mediator = mediator;
        _conversationsService = conversationsService;
    }

    public async Task Handle(UpdateConversationUnreadMessagesCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Update conversation participant unread messages for [UserId] = {command.User.GetId()} has started");

        var identity = command.User.MessagingIdentity();
        
        // Get the conversations that this user is a part of
        var conversations = await _conversationsService.GetByParticipantIdentity(identity);

        _logger.LogInformation($"Found  {conversations.Count()} for [UserId] = {command.User.GetId()}");
        
        foreach (var conversation in conversations)
        {
            await _mediator.Send(new UpdateMessageReadIndexesForConversationCommand(conversation.VendorExternalId));
        }
        
        _logger.LogInformation($"Update conversation participant unread messages for [UserId] = {command.User.GetId()} has completed");
    }
}