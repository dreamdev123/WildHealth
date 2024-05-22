using System;
using System.Collections.Generic;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Conversations.Flows;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Domain.PatientEngagements.Services;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.WebClient;
using static WildHealth.Domain.Entities.Engagement.EngagementCriteriaType;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class ProcessNewMessageNotificationFromTwilioCommandHandler : MessagingBaseService, IRequestHandler<ProcessNewMessageNotificationFromTwilioCommand>
    {
        private readonly IConversationsService _conversationService;
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly ILogger _logger;
        private readonly IConversationParticipantMessageSentIndexService _conversationMessageSentIndexService;
        private readonly MaterializeFlow _materializeFlow;
        private readonly IPatientEngagementService _engagementService;

        public ProcessNewMessageNotificationFromTwilioCommandHandler(
            IConversationsService conversationService,
            ISettingsManager settingsManager,
            ITwilioWebClient twilioWebClient,
            ILogger<ProcessNewMessageNotificationFromTwilioCommandHandler> logger,
            IConversationParticipantMessageSentIndexService conversationMessageSentIndexService, 
            MaterializeFlow materializeFlow, 
            IPatientEngagementService engagementService) : base(settingsManager)
        {
            _conversationService = conversationService;
            _twilioWebClient = twilioWebClient;
            _logger = logger;
            _conversationMessageSentIndexService = conversationMessageSentIndexService;
            _materializeFlow = materializeFlow;
            _engagementService = engagementService;
        }

        /// <summary>
        /// This handler will send clarity notification when new message in conversation is not opened
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Handle(ProcessNewMessageNotificationFromTwilioCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Started processing new message in Twilio from participant: {command.ParticipantSid} in conversation {command.ConversationSid} with index {command.Index}");
            var utcNow = DateTime.UtcNow;
            var conversation = await _conversationService.GetByExternalVendorIdSpecAsync(command.ConversationSid, ConversationSpecifications.Empty);
            var twilioParticipantsTask = GetConversationParticipantsFromTwilio(command, conversation); // start twilio get participants request but don't wait for result
            var messageSentIndexEntity = await _conversationMessageSentIndexService.GetByConversationAndParticipantAsync(command.ConversationSid, command.ParticipantSid);
            var (employees, patients) = await _conversationService.GetConversationParticipants(conversation.GetId());
            var twilioParticipants = await twilioParticipantsTask;
            var (receiverUserIds, senderId, participantUniversalId) = GetReceiverUserIds(command, conversation, employees.ToList(), patients.ToList(), twilioParticipants);
            var conversationEngagements = await _engagementService.GetActive(patients.Select(p => p.Patient.GetId()).ToArray(), NoVisitsOrMessagesSentIn14Days, PremiumMoreThan14DaysSinceLastClarityMessageReceived);
            
            var updateConversation = new NewConversationMessageFlow(conversation, employees.ToList(), patients.ToList(), senderId, command.Message, command.Index, conversationEngagements, utcNow);
            var bumpMessageSentIndex = new BumpMessageSentIndexFlow(messageSentIndexEntity, conversation.GetId(), command.ConversationSid, command.ParticipantSid, utcNow, command.Index, participantUniversalId);
            var notifyParticipants = new NotifyParticipantsFlow(conversation, employees, patients, command.Message, command.FileNames, receiverUserIds, senderId, participantUniversalId);
            
            await updateConversation.PipeTo(bumpMessageSentIndex).PipeTo(notifyParticipants).Materialize(_materializeFlow);

            _logger.LogInformation($"Finished processing new message in Twilio from participant: {command.ParticipantSid} in conversation {command.ConversationSid}");
        }

        private  (int[], int, Guid) GetReceiverUserIds(
            ProcessNewMessageNotificationFromTwilioCommand command,
            Conversation conversation, 
            ICollection<ConversationParticipantEmployee> employeeParticipants,
            ICollection<ConversationParticipantPatient> patientParticipants,
            ConversationParticipantResponseModel[] twilioParticipants)
        {
            var domain = new ConversationDomain(conversation, employeeParticipants, patientParticipants, _logger.LogInformation);
            var participants = twilioParticipants.Select(x => new TwilioParticipant(x.Sid, x.LastReadMessageIndex)).ToArray();
            var receiverUserIds = domain.GetReceiverUserIds(participants, command.ParticipantSid, command.Index).ToArray();
            var (senderId, participantUniversalId, _) = domain.GetParticipantIdentity(command.ParticipantSid);
            
            return (receiverUserIds, senderId, participantUniversalId);
        }

        private async Task<ConversationParticipantResponseModel[]> GetConversationParticipantsFromTwilio(
            ProcessNewMessageNotificationFromTwilioCommand command, 
            Conversation conversation)
        {
            var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);
            _twilioWebClient.Initialize(credentials);
            var participantResources = await _twilioWebClient.GetConversationParticipantResourcesAsync(command.ConversationSid);
        
            return participantResources.Participants.ToArray();
        }
    }
}
