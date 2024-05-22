using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Settings;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class SendMessageCommandHandler : MessagingBaseService, IRequestHandler<SendMessageCommand, ConversationMessageModel>
{
    private readonly IConversationsService _conversationsService;
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public SendMessageCommandHandler(
        ISettingsManager settingsManager,
        IConversationsService conversationsService, 
        ITwilioWebClient twilioWebClient, 
        IMediator mediator, 
        ILogger<SendMessageCommandHandler> logger): base(settingsManager)
    {
        _conversationsService = conversationsService;
        _twilioWebClient = twilioWebClient;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ConversationMessageModel> Handle(SendMessageCommand command, CancellationToken cancellationToken)
    {
        var conversation = command.Conversation;
        var author = command.Author;
        var body = command.Body;
        var media = command.Media;
        
        try
        {
            var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

            _twilioWebClient.Initialize(credentials);
            
            conversation = await _conversationsService.GetByExternalVendorIdAsync(conversation.VendorExternalId);
            
            var message = await _twilioWebClient.CreateConversationMessageAsync(new CreateConversationMessageModel
            {
                ConversationSid = conversation.VendorExternalId,
                Author = author,
                Body = body,
                MediaSid = media != null ? media.Sid : ""
            });

            var messagesResponse = await _twilioWebClient.GetMessagesAsync(conversation.VendorExternalId, MessagesOrderType.desc.ToString(), 1 );

            var updateLastMessageIndexCommand = new LastReadMessageUpdateCommand(
                conversationId: conversation.GetId(),
                conversationExternalVendorId: conversation.VendorExternalId,
                participantExternalVendorId: author,
                lastMessageReadIndex: messagesResponse.Messages.First().Index
            );
            
            await _mediator.Send(updateLastMessageIndexCommand, cancellationToken);

            return message;
        }
        catch (Exception err)
        {
            _logger.LogError($"Error creating message in Twilio with [ID]: {conversation.VendorExternalId} with error: {err}");

            throw;
        }
    }
}