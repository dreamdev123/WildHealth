using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Events.Subscriptions;
using WildHealth.Application.Services.Conversations;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.EventHandlers.Subscriptions;

public class ClearConversationsOnSubscriptionCancelledEvent : INotificationHandler<SubscriptionCancelledEvent>
{
    private readonly IConversationsService _conversationsService;
    private readonly IMediator _mediator;
    private readonly ILogger<ClearConversationsOnSubscriptionCancelledEvent> _logger;

    public ClearConversationsOnSubscriptionCancelledEvent(
        IConversationsService conversationsService,
        IMediator mediator,
        ILogger<ClearConversationsOnSubscriptionCancelledEvent> logger)
    {
        _conversationsService = conversationsService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(SubscriptionCancelledEvent notification, CancellationToken cancellationToken)
    {
        if (notification.IsRenewal) return;
        var patientId = notification.Patient.GetId();
        var conversations = await _conversationsService.GetAllConversationByPatientAsync(patientId);
        foreach (var conversation in conversations)
        {
            foreach (var employeeParticipant in conversation.EmployeeParticipants)
            {
                await _conversationsService.RemoveParticipantAsync(conversation, employeeParticipant);
            }

            if (conversation.Type != ConversationType.HealthCare)
            {
                var result = await _mediator.Send(
                    new UpdateStateConversationCommand(conversation.GetId(), ConversationState.Closed, 0),
                    cancellationToken).ToTry();
                
                result.DoIfError(ex => _logger.LogWarning(ex,
                    "Failed to close conversation on subscription cancelled. [ConversationId]: {ConversationId}, [SubscriptionId]: {SubscriptionId}",
                    conversation.GetId(), notification.Subscription.GetId()));
            }
        }
    }
}