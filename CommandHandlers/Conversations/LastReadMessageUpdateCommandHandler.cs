using System;
using System.Linq;
using System.Net;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Twilio.Clients.Models.ConversationParticipants;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Settings;
using WildHealth.Shared.Exceptions;


namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class LastReadMessageUpdateCommandHandler : MessagingBaseService, IRequestHandler<LastReadMessageUpdateCommand, ConversationParticipantMessageReadIndex>
    {
        private readonly IConversationParticipantMessageReadIndexService _conversationParticipantMessageReadIndexService;
        private readonly IConversationMessageUnreadNotificationService _conversationMessageUnreadNotificationService;
        private readonly IConversationsService _conversationsService;
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly IMediator _mediator;
        private readonly ILogger<LastReadMessageUpdateCommandHandler> _logger;

        public LastReadMessageUpdateCommandHandler(
            ISettingsManager settingsManager,
            ITwilioWebClient twilioWebClient,
            IMediator mediator,
            ILogger<LastReadMessageUpdateCommandHandler> logger,
            IConversationParticipantMessageReadIndexService conversationParticipantMessageReadIndexService, 
            IConversationMessageUnreadNotificationService conversationMessageUnreadNotificationService,
            IConversationsService conversationsService) : base(settingsManager)
        {
            _twilioWebClient = twilioWebClient;
            _mediator = mediator;
            _logger = logger;
            _conversationParticipantMessageReadIndexService = conversationParticipantMessageReadIndexService;
            _conversationMessageUnreadNotificationService = conversationMessageUnreadNotificationService;
            _conversationsService = conversationsService;
        }

        /// <summary>
        /// This handler will send clarity notification when new message will be sent if conversation is not opened
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConversationParticipantMessageReadIndex> Handle(LastReadMessageUpdateCommand request, CancellationToken cancellationToken)
        {
            Conversation? conversation = null;

            try
            {
                // Protect against a scenario where the conversation Id provided does not exist in Twilio
                conversation =
                    await _conversationsService.GetByExternalVendorIdTrackAsync(request.ConversationExternalVendorId);
            }
            catch (AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return ConversationParticipantMessageReadIndex.Default();
            }
            var conversationDomain = ConversationDomain.Create(conversation);
            
            // If we find that the read index passed is much larger than the conversation index, we need to raise concern
            if ((conversation.Index + 2) < request.LastMessageReadIndex)
            {
                _logger.LogWarning($"Received incorrect request to update [ReadIndex] = {request.LastMessageReadIndex} for [ConversationId] = {request.ConversationId} and [ParticipantSid] = {request.ParticipantExternalVendorId}");
                
                return ConversationParticipantMessageReadIndex.Default();
            }
            
            
            var model = await _conversationParticipantMessageReadIndexService.GetByConversationAndParticipantAsync(
                request.ConversationExternalVendorId,
                request.ParticipantExternalVendorId
            );
            
            if (model is null) 
            {
                model = ConversationParticipantMessageReadIndex.Create(
                        conversationId: request.ConversationId,
                        conversationVendorExternalId: request.ConversationExternalVendorId, 
                        participantVendorExternalId: request.ParticipantExternalVendorId, 
                        lastReadIndex: request.LastMessageReadIndex,
                        participantIdentity: new Guid(request.ParticipantExternalVendorId));
                            
                await _conversationParticipantMessageReadIndexService.CreateAsync(model);
            } 
            else 
            {
                if (request.LastMessageReadIndex > model.LastReadIndex)
                {
                    model.SetLastMessageReadIndex(request.LastMessageReadIndex);

                    await _conversationParticipantMessageReadIndexService
                        .UpdateAsync(model);
                }
            }

            var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

            _twilioWebClient.Initialize(credentials);
            
            // Tell Twilio that this index has been read by the participant
            // get the participant id instead of vendorExternalIdentity
            var conversationParticipantEmployee = conversation.EmployeeParticipants.FirstOrDefault(emp => emp.VendorExternalIdentity == request.ParticipantExternalVendorId);

            var conversationParticipantPatient = conversation.PatientParticipants.FirstOrDefault(pat => pat.VendorExternalIdentity == request.ParticipantExternalVendorId);

            var participantVendorExternalId = conversationParticipantEmployee?.GetCurrentVendorExternalId() ??
                                              conversationParticipantPatient?.VendorExternalId;

            // If it's null but we have an employee participant, we need to try and recover this identifier
            if (conversationParticipantEmployee is not null && string.IsNullOrEmpty(participantVendorExternalId))
            {
                conversationParticipantEmployee = await _mediator.Send(new RecoverParticipantExternalIdCommand(
                    conversationSid: request.ConversationExternalVendorId,
                    identity: request.ParticipantExternalVendorId));

                participantVendorExternalId = conversationParticipantEmployee?.GetCurrentVendorExternalId();
            }
            
            try
            {
                await _twilioWebClient.UpdateConversationParticipantAsync(new UpdateConversationParticipantModel(
                    sid: participantVendorExternalId,
                    conversationSid: request.ConversationExternalVendorId,
                    lastReadMessageIndex: model.LastReadIndex
                ));
            }
            catch (Twilio.Clients.Exceptions.TwilioException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                // Want to update the VendorExternalId to null if Twilio says it does not exist
                await RemoveParticipantVendorExternalId(conversation, participantVendorExternalId);
            }

            // Also determine if this last read value nullifies a notification of unread messages
            var notifications = await _conversationMessageUnreadNotificationService.GetOutstandingNotificationsAsync(
                request.ConversationExternalVendorId, 
                request.ParticipantExternalVendorId
            );

            foreach (var notification in notifications) 
            {
                if (notification.LastReadMessageIndex <= request.LastMessageReadIndex) 
                {
                    notification.SetIsRead(true);

                    await _conversationMessageUnreadNotificationService.UpdateAsync(notification);
                }
            }

            // Update the conversation if the last index in there is less than this index
            if(conversation.Index < request.LastMessageReadIndex) 
            {   
                conversationDomain.SetIndex(request.LastMessageReadIndex);
                
                await _conversationsService.UpdateConversationAsync(conversation);
            }

            if (!conversation.HasMessages) 
            {
                conversationDomain.SetHasMessages(true);
                
                await _conversationsService.UpdateConversationAsync(conversation);
            }

            return model;
        }

        private async Task RemoveParticipantVendorExternalId(Conversation conversation,
            string? participantVendorExternalId)
        {
            var employeeParticipant =
                conversation.EmployeeParticipants.FirstOrDefault(o =>
                    o.VendorExternalId == participantVendorExternalId);

            if (employeeParticipant is not null)
            {
                employeeParticipant.SetVendorExternalId(null);

                await _conversationsService.UpdateConversationAsync(conversation);

                return;
            }
            
            
            var patientParticipant =
                conversation.PatientParticipants.FirstOrDefault(o =>
                    o.VendorExternalId == participantVendorExternalId);

            if (patientParticipant is not null)
            {
                patientParticipant.SetVendorExternalId(null);

                await _conversationsService.UpdateConversationAsync(conversation);
            }
        }
    }
}
