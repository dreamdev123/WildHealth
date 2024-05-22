using System;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Commands.Documents;
using WildHealth.Domain.Constants;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Ai;
using WildHealth.IntegrationEvents.Ai.Payloads;
using WildHealth.Jenny.Clients.WebClients;
using WildHealth.Twilio.Clients.Enums;

namespace WildHealth.Application.IntegrationEventHandlers;

public class AiIntegrationEventHandler : IEventHandler<AiIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly IJennyConversationWebClient _jennyConversationWebClient;
    
    public AiIntegrationEventHandler(
        IMediator mediator,
        IJennyConversationWebClient jennyConversationWebClient
        )
    {
        _mediator = mediator;
        _jennyConversationWebClient = jennyConversationWebClient;
    }

    public async Task Handle(AiIntegrationEvent @event)
    {
        switch (@event.PayloadType)
        {
            case nameof(HcAssistResponseGeneratedPayload):
                await HandleHcAssistResponseGenerated(@event.DeserializePayload<HcAssistResponseGeneratedPayload>());
                break;
           
            default:
                throw new ArgumentOutOfRangeException($"Unknown AiIntegrationEvent payload type of {@event.PayloadFullType}");
        }
    }
    
    #region private

    private async Task HandleHcAssistResponseGenerated(HcAssistResponseGeneratedPayload payload)
    {
        var responseModel = await _jennyConversationWebClient.GetHealthCoachAssistResult(payload.RequestId);
        
        await _mediator.Send(new AddInteractionCommand(
            conversationId: payload.ConversationSid,
            messageId: payload.MessageSid,
            referenceId: responseModel.Id,
            participantId: AiConstants.AiParticipantIdentifier,
            detail: responseModel.Text,
            type: InteractionType.Recommendation,
            userUniversalId: String.Empty
        ));
    }

    #endregion
}
