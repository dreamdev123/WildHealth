using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class UpdateMessageSentIndexesForConversationCommandHandler : MessagingBaseService, IRequestHandler<UpdateMessageSentIndexesForConversationCommand>
{
    private readonly int _numberDaysBackToProcess = 3;
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly IConversationsService _conversationsService;
    private readonly ILogger<SendScheduledMessagesCommandHandler> _logger;
    private readonly IMediator _mediator;
    
    public UpdateMessageSentIndexesForConversationCommandHandler(
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

    /// <summary>
    /// Assumption is that each participant has contributed in at least one of the last 100 messages sent
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task Handle(UpdateMessageSentIndexesForConversationCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Update message sent indexes for [ConversationSid] = {command.ConversationSid} has started");

        var conversation = await _conversationsService.GetByExternalVendorIdAsync(command.ConversationSid, true);

        var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

        _twilioWebClient.Initialize(credentials);

        var messagesResponse = await _twilioWebClient.GetMessagesAsync(conversation.VendorExternalId, MessagesOrderType.desc.ToString(), 200);

        if (!AreThereAnyNewMessages(messagesResponse))
        {
            return;
        }

        foreach (var authorMessages in messagesResponse.Messages.OrderByDescending(o => o.Index).GroupBy(o => o.ParticipantSid))
        {
            var participantSid = authorMessages.Key;
            
            var mostRecentMessage = authorMessages.FirstOrDefault();

            if (mostRecentMessage is null || String.IsNullOrEmpty(participantSid))
            {
                continue;
            }

            var (_, senderUniversalId, isSuccess) = await _mediator.Send(new GetConversationParticipantIdentityCommand(
                conversationId: conversation.GetId(),
                participantSid: participantSid));

            if (isSuccess)
            {
                var updateMessageSentIndexCommand = new UpdateConversationsMessageSentIndexCommand(
                    conversationId: conversation.GetId(),
                    conversationVendorExternalId: command.ConversationSid,
                    participantVendorExternalId: participantSid,
                    index: mostRecentMessage.Index,
                    createdAt: mostRecentMessage.DateCreated,
                    participantIdentity: senderUniversalId);
                
                await _mediator.Send(updateMessageSentIndexCommand, cancellationToken);
            }
        }

        _logger.LogInformation($"Update message sent indexes for [ConversationSid] = {command.ConversationSid} has finished");
    }

    /// <summary>
    /// Checks to make sure there's at least one message and the message was created within the last _numberDaysBackToProcess days (done in case this script fails one night)
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    public bool AreThereAnyNewMessages(ConversationMessagesModel response)
    {
        return response.Messages.Any() && response.Messages.OrderByDescending(o => o.Index).First().DateCreated.AddDays(_numberDaysBackToProcess) > DateTime.UtcNow;
    }
}