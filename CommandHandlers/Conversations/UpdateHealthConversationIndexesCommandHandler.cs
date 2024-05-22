using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class UpdateHealthConversationIndexesCommandHandler : MessagingBaseService, IRequestHandler<UpdateHealthConversationIndexesCommand>
{
    private readonly int LAST_MESSAGE_SENT_WITHIN_HOURS = 26;
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly IConversationsService _conversationsService;
    private readonly ILogger<SendScheduledMessagesCommandHandler> _logger;
    private readonly IMediator _mediator;
    
    public UpdateHealthConversationIndexesCommandHandler(
        ISettingsManager settingsManager,
        ITwilioWebClient twilioWebClient,
        ILogger<SendScheduledMessagesCommandHandler> logger,
        IConversationsService conversationsService,
        IMediator mediator
    ) : base(settingsManager)
    {
        _twilioWebClient = twilioWebClient;
        _logger = logger;
        _conversationsService = conversationsService;
        _mediator = mediator;

    }

    public async Task Handle(UpdateHealthConversationIndexesCommand command, CancellationToken cancellationToken)
    {
        // Want to get conversations that have a message sent within the last 26 hours.  This ensures that the daily
        // script will pick up anything new and provide a little overlap for fault tolerance
        var conversations = await _conversationsService.GetAllActiveWithMessageSentSince(DateTime.UtcNow.AddHours(-LAST_MESSAGE_SENT_WITHIN_HOURS));

        foreach(var conversation in conversations)
        {
            await _mediator.Send(new UpdateMessageSentIndexesForConversationCommand(conversation.VendorExternalId));
            await _mediator.Send(new UpdateMessageReadIndexesForConversationCommand(conversation.VendorExternalId));
            await _mediator.Send(new RemoveOrphanedParticipantsCommand(conversationSid: conversation.VendorExternalId));
        }
    }
}