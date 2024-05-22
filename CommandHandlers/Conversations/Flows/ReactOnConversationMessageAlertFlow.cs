using System;
using System.Collections.Generic;
using WildHealth.Twilio.Clients.Models.Conversations.Alerts;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Exceptions;
using System.Linq;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Conversations.Flows;

public record ReactOnConversationMessageAlertFlow(
    MessageAlertModel Alert, 
    MessageAlertActionType ActionType,
    string Details,
    User ReactedBy,
    DateTime UtcNow) : IMaterialisableFlow
{
    private readonly IDictionary<MessageAlertType, MessageAlertActionType[]> _availableActions =
        new Dictionary<MessageAlertType, MessageAlertActionType[]>
        {
            {
                MessageAlertType.TicketRequestAlert,
                new[]
                {
                    MessageAlertActionType.Accepted,
                    MessageAlertActionType.Rejected
                }
            }
        };

    public MaterialisableFlowResult Execute()
    {
        AssertCanReactOnAlert();

        return Act();
    }

    #region private

    private MaterialisableFlowResult Act()
    {
        return (Alert.Type, ActionType) switch
        {
            (MessageAlertType.TicketRequestAlert, MessageAlertActionType.Rejected) => StoreAction(),
            (MessageAlertType.TicketRequestAlert, MessageAlertActionType.Accepted) => StoreAction() + CreateSupportTicket(),
            _ => MaterialisableFlowResult.Empty
        };
    }

    private MaterialisableFlowResult StoreAction()
    {
        var action = new MessageAlertActionModel
        {
            ReactedAt = UtcNow,
            ReactedBy = ReactedBy.GetId(),
            Details = Details,
            Type = ActionType
        };
        
        Alert.Actions.Add(action);
        
        return MaterialisableFlowResult.Empty;
    }

    private INotification CreateSupportTicket() => new TicketRequestAlertAcceptedEvent(
        patientId: Alert.GetDataValue<int>(nameof(TicketRequestAlertAcceptedEvent.PatientId)),
        locationId: Alert.GetDataValue<int>(nameof(TicketRequestAlertAcceptedEvent.LocationId)),
        practiceId: Alert.GetDataValue<int>(nameof(TicketRequestAlertAcceptedEvent.PracticeId)),
        subject: Alert.GetDataValue<string>(nameof(TicketRequestAlertAcceptedEvent.Subject))
    );

    private void AssertCanReactOnAlert()
    {
        if (Alert.ExpiresAt < UtcNow)
        {
            throw new DomainException("Alert is expired");
        }

        if (Alert.Audience.Any(x => x.Type == ReactedBy.Identity.Type.ToString()))
        {
            throw new DomainException("Alert is now available for this user");
        }

        if (Alert.Actions.Any(x => x.ReactedBy == ReactedBy.GetId()))
        {
            throw new DomainException("Alert hes been reacted already");
        }

        if (!_availableActions[Alert.Type].Contains(ActionType))
        {
            throw new DomainException("Alert does not support this action");
        }
    }

    #endregion
}