using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class CheckEmployeeUnreadMessagesCommandHandler : MessagingBaseService, IRequestHandler<CheckEmployeeUnreadMessagesCommand>
{
    private readonly IConversationsService _conversationsService;
    private readonly ILogger<CheckEmployeeUnreadMessagesCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ITwilioWebClient _twilioWebClient;

    public CheckEmployeeUnreadMessagesCommandHandler(
        ISettingsManager settingsManager,
        IConversationsService conversationsService,
        ILogger<CheckEmployeeUnreadMessagesCommandHandler> logger,
        IMediator mediator,
        ITwilioWebClient twilioWebClient
        ) : base(settingsManager)
    {
        _conversationsService = conversationsService;
        _logger = logger;
        _mediator = mediator;
        _twilioWebClient = twilioWebClient;
    }

    public async Task Handle(CheckEmployeeUnreadMessagesCommand command, CancellationToken cancellationToken)
    {
        var credentials = await GetMessagingCredentialsAsync(command.PracticeId);

        _twilioWebClient.Initialize(credentials);

        var models = await _conversationsService.EmployeeUnreadMessages(command.PracticeId);

        foreach (var model in models)
        {
            var conversation = await _conversationsService.GetByIdAsync(model.ConversationId);
            
            // Get conversation participants
            var participants = await _twilioWebClient.GetConversationParticipantResourcesAsync(model.ConversationSid);

            var messagesResponse = await _twilioWebClient.GetMessagesAsync(conversation.VendorExternalId, MessagesOrderType.desc.ToString(), 100);

            var hasMessages = messagesResponse.Messages.Any();
            var currentTwilioIndex = hasMessages ? messagesResponse.Messages.First().Index : 0;
            
            var thisParticipant =
                participants.Participants.FirstOrDefault(o => o.Identity == model.EmployeeUniversalId.ToString());

            // If the participant isn't there, then it means they got removed, we want to remove on our side as well.
            if (thisParticipant is null)
            {
                await _mediator.Send(
                    new RemoveEmployeeParticipantFromConversationCommand(model.ConversationId, model.EmployeeUserId));

                continue;
            }
            
            var correctIndex = thisParticipant.LastReadMessageIndex ?? -1;
            
            // Make sure read indexes are aligned, if not then update this
            if (correctIndex != model.EmployeeLastReadIndex)
            {
                await _mediator.Send(new LastReadMessageUpdateCommand(
                    conversationId: model.ConversationId,
                    conversationExternalVendorId: model.ConversationSid,
                    participantExternalVendorId: model.EmployeeUniversalId.ToString(),
                    lastMessageReadIndex: correctIndex));
            }
            
            // Make sure the conversation index is aligned
            if (currentTwilioIndex != conversation.Index)
            {
                var conversationDomain = ConversationDomain.Create(conversation);
                
                conversationDomain.SetIndex(currentTwilioIndex, true);
                conversationDomain.SetHasMessages(hasMessages);
                await _conversationsService.UpdateConversationAsync(conversation);
            }
        }
    }
}