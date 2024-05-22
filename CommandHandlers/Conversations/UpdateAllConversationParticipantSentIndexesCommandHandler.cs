using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.WebClient;
using Microsoft.Extensions.Logging;
using MediatR;
using WildHealth.Application.CommandHandlers.Conversations.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class UpdateAllConversationParticipantSentIndexesCommandHandler : MessagingBaseService, IRequestHandler<UpdateAllConversationParticipantSentIndexesCommand>
    {
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly IConversationParticipantMessageSentIndexService _conversationParticipantMessageSentIndexService;
        private readonly IConversationsService _conversationsService;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly MaterializeFlow _materializeFlow;
        private readonly IConversationParticipantMessageSentIndexService _conversationMessageSentIndexService;

        public UpdateAllConversationParticipantSentIndexesCommandHandler(
            ITwilioWebClient twilioWebClient,
            IConversationParticipantMessageSentIndexService conversationParticipantMessageSentIndexService,
            IConversationsService conversationsService,
            ISettingsManager settingsManager,
            ILogger<UpdateAllConversationParticipantSentIndexesCommandHandler> logger,
            IMediator mediator, 
            IConversationParticipantMessageSentIndexService conversationMessageSentIndexService, 
            MaterializeFlow materializeFlow) : base(settingsManager)
        {
            _twilioWebClient = twilioWebClient;
            _conversationParticipantMessageSentIndexService = conversationParticipantMessageSentIndexService;
            _conversationsService = conversationsService;
            _logger = logger;
            _mediator = mediator;
            _conversationMessageSentIndexService = conversationMessageSentIndexService;
            _materializeFlow = materializeFlow;
        }

        public async Task Handle(UpdateAllConversationParticipantSentIndexesCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Started updating conversation participant sent message indexes for [ExternalVendorId] = {command.ConversationVendorExternalId}");

            var conversation = await _conversationsService.GetByExternalVendorIdAsync(command.ConversationVendorExternalId);

            var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

            _twilioWebClient.Initialize(credentials);
            
            // var conversation = await _twilioWebClient.GetConversationAsync(command.ConversationVendorExternalId);
            
            var participantsResponse = await _twilioWebClient.GetConversationParticipantResourcesAsync(command.ConversationVendorExternalId);

            var messagesResponse = await _twilioWebClient.GetMessagesAsync(command.ConversationVendorExternalId,
                MessagesOrderType.desc.ToString(), 500);
            
            foreach (var participant in participantsResponse.Participants)
            {
                var participantSid = participant.Sid;
                
                _logger.LogInformation(
                    $"Updating last sent message information for [ConversationId] = {command.ConversationVendorExternalId}, [ParticipantId] = {participantSid}");
                
                try
                {
                    // Get the participants' most recent message
                    var mostRecentMessage = messagesResponse.Messages.Where(o => o.ParticipantSid == participantSid)
                        .FirstOrDefault();

                    if (mostRecentMessage == null)
                    {
                        _logger.LogInformation(
                            $"Unable to find a message in the conversation for [ConversationId] = {command.ConversationVendorExternalId}, [ParticipantId] = {participantSid}");

                        continue;
                    }
                        
                    var (_, participantUniversalId, isSuccess) = await _mediator.Send(new GetConversationParticipantIdentityCommand(
                        conversationId: conversation.GetId(),
                        participantSid: participant.Sid));

                    if (isSuccess)
                    {
                        var messageSentIndexEntity = await _conversationMessageSentIndexService.GetByConversationAndParticipantAsync(command.ConversationVendorExternalId, participantSid);
                        
                        await new BumpMessageSentIndexFlow(
                            messageSentIndexEntity,
                            conversationId: conversation.GetId(),
                            command.ConversationVendorExternalId,
                            participantVendorExternalId: participantSid,
                            mostRecentMessage.DateCreated,
                            mostRecentMessage.Index,
                            participantUniversalId).Materialize(_materializeFlow);
                    }
                    
                    _logger.LogInformation(
                        $"Updated last sent message information for [ConversationId] = {command.ConversationVendorExternalId}, [ParticipantId] = {participantSid}, [DateCreated] = {mostRecentMessage.DateCreated}");
                }
                catch(Exception ex)
                {
                    _logger.LogError($"Error during updating conversation {conversation.GetId()} indexes: {ex}");
                }
            }
            
            _logger.LogInformation($"Finished updating conversation participant sent message indexes for [ExternalVendorId] = {command.ConversationVendorExternalId}");
        }
    }
}