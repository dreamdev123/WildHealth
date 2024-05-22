using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.WebClient;
using Microsoft.Extensions.Logging;
using MediatR;
using WildHealth.Domain.Models.Conversation;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class UpdateConversationsIndexCommandHandler : MessagingBaseService, IRequestHandler<UpdateConversationsIndexCommand> 
    {
        private readonly IConversationParticipantMessageReadIndexService _conversationParticipantMessageReadIndexService;
        private readonly IConversationsService _conversationsService;
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;

        public UpdateConversationsIndexCommandHandler(
            IConversationParticipantMessageReadIndexService conversationParticipantMessageReadIndexService,
            IConversationsService conversationsService,
            ISettingsManager settingsManager,
            ITwilioWebClient twilioWebClient,
            ILogger<UpdateConversationsIndexCommandHandler> logger,
            IMediator mediator) : base(settingsManager)
        {
            _conversationParticipantMessageReadIndexService = conversationParticipantMessageReadIndexService;
            _conversationsService = conversationsService;
            _twilioWebClient = twilioWebClient;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task Handle(UpdateConversationsIndexCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Started updating conversation indexes");
            
            var conversations = (await _conversationsService.GetAllActiveAsync())
                .Where(o => !string.IsNullOrEmpty(o.VendorExternalId));

            foreach (var conversation in conversations)
            {
                try
                {
                    var conversationDomain = ConversationDomain.Create(conversation);
                    var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

                    _twilioWebClient.Initialize(credentials);

                    var messagesFromConversation = await _twilioWebClient.GetMessagesAsync(conversation.VendorExternalId, MessagesOrderType.desc.ToString(), 1);

                    var lastMessage = messagesFromConversation.Messages.FirstOrDefault();

                    var index = lastMessage?.Index ?? 0;

                    conversationDomain.SetIndex(index);
                    
                    conversationDomain.SetHasMessages(true);

                    await _conversationsService.UpdateConversationAsync(conversation);

                    /////////////////////////////////////////////////////////
                    // Get each participant and update last read index
                    /////////////////////////////////////////////////////////
                    var participantResources = await _twilioWebClient.GetConversationParticipantResourcesAsync(conversation.VendorExternalId);

                    foreach (var participant in participantResources.Participants)
                    {
                        if(participant.LastReadMessageIndex == null) {
                            continue;
                        }
                        
                        var conversationExternalId = conversation.VendorExternalId;
                        var participantExternalVendorId = participant.Identity;
                        var lastReadMessageIndex = participant.LastReadMessageIndex.Value;
                        var lastReadMessageSentDate = DateTime.Parse(participant.LastReadTimestamp);

                        var model = await _conversationParticipantMessageReadIndexService.GetByConversationAndParticipantAsync(
                            conversationExternalId,
                            participantExternalVendorId
                        );

                        if (model is null) 
                        {
                            var (senderId, participantUniversalId, isSuccess) = await _mediator.Send(new GetConversationParticipantIdentityCommand(
                                conversationId: conversation.GetId(),
                                participantSid: participant.Sid));

                            if (isSuccess)
                            {
                                model = ConversationParticipantMessageReadIndex.Create(
                                    conversationId: conversation.GetId(),
                                    conversationVendorExternalId: conversationExternalId, 
                                    participantVendorExternalId: participantExternalVendorId, 
                                    lastReadIndex: lastReadMessageIndex,
                                    participantIdentity: participantUniversalId);
                                        
                                await _conversationParticipantMessageReadIndexService.CreateAsync(model);
                            }
                        } 
                        else 
                        {
                            model.SetLastMessageReadIndex(lastReadMessageIndex);

                            await _conversationParticipantMessageReadIndexService
                                .UpdateAsync(model);
                        }
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError($"Error during updating conversation {conversation.GetId()} indexes: with error {ex.ToString()}");
                }
            }
            
            _logger.LogInformation("Finished updating conversation indexes");
        }
    }
}