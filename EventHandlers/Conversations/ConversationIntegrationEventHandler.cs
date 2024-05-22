using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Durable.Mediator;
using WildHealth.Application.EventHandlers.Ai;
using WildHealth.Application.Events.Ai;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.ConversationMessages;
using WildHealth.IntegrationEvents.ConversationMessages.Payloads;

namespace WildHealth.Application.EventHandlers.Conversations
{
    public class ConversationIntegrationEventHandler : IEventHandler<ConversationMessageIntegrationEvent>
    {
        private readonly IMediator _mediator;
        private readonly IDurableMediator _durableMediator;
        private readonly ILogger _logger;
        
        public ConversationIntegrationEventHandler(
            IMediator mediator,
            IDurableMediator durableMediator,
            ILogger<ConversationIntegrationEventHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
            _durableMediator = durableMediator;
        }
        
        public async Task Handle(ConversationMessageIntegrationEvent @event)
        {
            _logger.LogInformation($"Started processing conversation integration event {@event.Id} with payload type: {@event.PayloadType} - {@event.Payload}");
            
            try
            {
                switch (@event.PayloadType)
                {
                    case nameof(ConversationMessageAddedPayload): 
                        await ProcessConversationMessageAddedPayload(@event.Payload as ConversationMessageAddedPayload ?? 
                                                                     JsonConvert.DeserializeObject<ConversationMessageAddedPayload>(@event.Payload.ToString()),
                                                                     @event.User.UniversalId); 
                        break;
                    
                    case nameof(ConversationMessageReadPayload): 
                        await ProcessConversationMessageReadPayload(JsonConvert.DeserializeObject<ConversationMessageReadPayload>(@event.Payload.ToString())); 
                        break;
                    
                    default: throw new ArgumentException("Unsupported conversation integration event payload");
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed processing conversation integration event {@event.Id} with payload: {@event.Payload}. {e}");
                throw;
            }
            
            _logger.LogInformation($"Processed conversation integration event {@event.Id} with payload: {@event.Payload}");
        }
        
        #region private

        private async Task ProcessConversationMessageAddedPayload(ConversationMessageAddedPayload payload, string userId)
        {
            // process the message
            var processMessageCommand = new ProcessNewMessageNotificationFromTwilioCommand(
                payload.ConversationSid,
                payload.ParticipantSid,
                payload.Index,
                payload.MediaAttached,
                payload.Body
            );
            await _mediator.Send(processMessageCommand);

            var aiEvent = new AiConversationMessageAddedEvent(payload.ConversationSid, payload.MessageSid, userId);
            await _durableMediator.Publish(aiEvent);
        }

        private async Task ProcessConversationMessageReadPayload(ConversationMessageReadPayload payload)
        {
            var command = new LastReadMessageUpdateCommand(
                conversationId: payload.ConversationId,
                conversationExternalVendorId: payload.ConversationExternalVendorId,
                participantExternalVendorId: payload.ParticipantExternalVendorId,
                lastMessageReadIndex: Convert.ToInt32(payload.LastMessageReadIndex)
            );
            await _mediator.Send(command);
        }
        
        #endregion
    }
}