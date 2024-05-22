using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Twilio.Exceptions;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.Models.ConversationParticipants;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class UpdateMessageReadIndexesForConversationCommandHandler : MessagingBaseService, IRequestHandler<UpdateMessageReadIndexesForConversationCommand>
{
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly IConversationsService _conversationsService;
    private readonly ILogger<SendScheduledMessagesCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IConversationParticipantMessageReadIndexService _conversationParticipantMessageReadIndexService;
    
    public UpdateMessageReadIndexesForConversationCommandHandler(
        ISettingsManager settingsManager,
        ITwilioWebClient twilioWebClient,
        ILogger<SendScheduledMessagesCommandHandler> logger,
        IMediator mediator,
        IConversationsService conversationsService,
        IConversationParticipantMessageReadIndexService conversationParticipantMessageReadIndexService
    ) : base(settingsManager)
    {
        _twilioWebClient = twilioWebClient;
        _logger = logger;
        _mediator = mediator;
        _conversationsService = conversationsService;
        _conversationParticipantMessageReadIndexService = conversationParticipantMessageReadIndexService;

    }

    public async Task Handle(UpdateMessageReadIndexesForConversationCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Update message read indexes for [ConversationSid] = {command.ConversationSid} has started");

        var conversation = await _conversationsService.GetByExternalVendorIdAsync(command.ConversationSid, true);
        var conversationDomain = ConversationDomain.Create(conversation);

        // Initialize the web client
        var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

        _twilioWebClient.Initialize(credentials);

        _logger.LogInformation($"Getting all participants for [ConversationId] = {conversation.GetId()}");
        
        // Get all of the participants
        var participantsResponse =
            await _twilioWebClient.GetConversationParticipantResourcesAsync(conversation.VendorExternalId);

        var messagesResponse = await _twilioWebClient.GetMessagesAsync(conversation.VendorExternalId, MessagesOrderType.desc.ToString(), 100);

        var hasMessages = messagesResponse.Messages.Any();
        var currentIndex = hasMessages ? messagesResponse.Messages.First().Index : 0;
        
        // Update the conversation information first
        conversationDomain.SetIndex(currentIndex, true);
        conversationDomain.SetHasMessages(hasMessages);
        await _conversationsService.UpdateConversationAsync(conversation);

        var localEmployeeConversationParticipantsNotInTwilio = conversation.EmployeeParticipants;

        foreach (var participant in participantsResponse.Participants)
        {
            _logger.LogInformation($"Updating last read message index for [ConversationSid] = {participant.ConversationSid}, [ParticipantSid] = {participant.Sid}");

            // Check these off the list
            localEmployeeConversationParticipantsNotInTwilio =
                localEmployeeConversationParticipantsNotInTwilio
                    .Where(o => o.VendorExternalIdentity != participant.Identity)
                    .ToArray();
            
            var currentIndexRecord =
                await _conversationParticipantMessageReadIndexService.GetByConversationAndParticipantAsync(conversationExternalVendorId: participant.ConversationSid, participantExternalVendorId: participant.Identity);

            var newIndex = GetUnreadIndexBasedOnConversationAndParticipant(conversation, participant);

            // If
            // 1. Twilio says the read index is more than the actual conversation index,
            // 2. Our readIndex in the DB is <= the current conversation Index,
            // then we need to ignore any update
            // If index in our DB is too big, then we need to set it to the current conversation index
            if (newIndex > conversation.Index)
            {
                if (currentIndexRecord != null && currentIndexRecord.LastReadIndex <= conversation.Index)
                {
                    _logger.LogInformation($"Twilio says that the [ParticipantIndex] = {newIndex} for [ParticipantSid] = {participant.Sid} and [ConversationSid] = {participant.ConversationSid}");

                    continue;
                }
                
                // We need to set the newIndex to the current conversation Index so this bug does not persist
                newIndex = conversation.Index;
            }
            
            _logger.LogInformation($"Setting [ParticipantIndex] = {newIndex} for [ParticipantSid] = {participant.Sid} and [ConversationSid] = {participant.Sid}");

            var newSentDateTime = GetUnreadDateTimeBasedOnConversationAndParticipant(conversation, participant);
            
            if (currentIndexRecord == null)
            {
                var (senderId, participantUniversalId, isSuccess) = await _mediator.Send(new GetConversationParticipantIdentityCommand(
                    conversationId: conversation.GetId(),
                    participantSid: participant.Sid));
                    
                _logger.LogInformation($"Creating a new record for [ConversationId] = {conversation.GetId()}, [ParticipantSid] = {participant.Sid}");

                if (!isSuccess)
                {
                    _logger.LogWarning($"Unable to locate the participant information with the given [ConversationId] = {conversation.GetId()}, [ParticipantSid] = {participant.Sid}, going to continue to the next");
                    
                    continue;
                }
                

                // Intentionally want to set this to -1.  This means no messages have been read and since this is 0-index we will get proper math when subtraction current message index from last message read
                currentIndexRecord = ConversationParticipantMessageReadIndex.Create(
                    conversationId: conversation.GetId(),
                    participantVendorExternalId: participant.Identity,
                    conversationVendorExternalId: participant.ConversationSid,
                    lastReadIndex: newIndex,
                    participantIdentity: participantUniversalId
                );
            }
            
            var priorIndex = currentIndexRecord.LastReadIndex;
        
            _logger.LogInformation($"Updating a record for [ConversationSid] = {participant.ConversationSid}, [ParticipantSid] = {participant.Sid} to [Index] = {newIndex}");

            // Related to known bug
            // https://wildhealth.atlassian.net/browse/CLAR-6021
            // If we find that the read index is different than the conversation index, we need to update in Twilio
            if (priorIndex != newIndex && newIndex >= 0)
            {
                try
                {
                    await _twilioWebClient.UpdateConversationParticipantAsync(new UpdateConversationParticipantModel(
                        sid: participant.Sid,
                        conversationSid: participant.ConversationSid,
                        lastReadMessageIndex: newIndex));
                }
                catch (TwilioException ex)
                {
                    _logger.LogInformation($"Unable to handle update for [ParticipantSid] = {participant.Sid} - {ex}");
                }
            }
            
            currentIndexRecord.SetLastMessageReadIndex(newIndex);
            await _conversationParticipantMessageReadIndexService.UpdateAsync(currentIndexRecord);
            
        }

        // There's a scenario where the bug caused indexes to be off and those participants have since been removed from conversations, update that information locally here
        foreach (var participant in localEmployeeConversationParticipantsNotInTwilio)
        {
            var currentIndexRecord =
                await _conversationParticipantMessageReadIndexService.GetByConversationAndParticipantIdentityAsync(conversationVendorExternalId: conversation.VendorExternalId, participantVendorExternalIdentity: participant.VendorExternalIdentity);

            if (currentIndexRecord is null)
            {
                continue;
            }

            if (conversation.Index < currentIndexRecord.LastReadIndex)
            {
                currentIndexRecord.SetLastMessageReadIndex(conversation.Index);
                
                await _conversationParticipantMessageReadIndexService.UpdateAsync(currentIndexRecord);
            }
        }
        
        _logger.LogInformation($"Update message read indexes for [ConversationSid] = {command.ConversationSid} has finished");
    }
    
    private int GetUnreadIndexBasedOnConversationAndParticipant(Conversation conversation,
        ConversationParticipantResponseModel participant)
    {   
        return participant.LastReadMessageIndex.HasValue ? participant.LastReadMessageIndex.Value :
            conversation.Index > 0 ? -1 : 0;
    }
    private DateTime GetUnreadDateTimeBasedOnConversationAndParticipant(Conversation conversation,
        ConversationParticipantResponseModel participant)
    {
        return participant.LastReadTimestamp != null ? DateTime.Parse(participant.LastReadTimestamp) :
            DateTime.MinValue;
    }
}