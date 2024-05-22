using System;
using MediatR;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Conversation;

namespace WildHealth.Application.CommandHandlers.Conversations.Flows;

public class UpdateConversationsSettingsFlow : IMaterialisableFlow
{
    private readonly ConversationsSettings _settings;
    private readonly bool _awayMessageEnabled;    
    private readonly DateTime? _awayMessageEnabledFrom;
    private readonly DateTime? _awayMessageEnabledTo;
    private readonly ConversationAwayMessageTemplate? _awayMessageTemplate;
    private readonly bool _forwardEmployeeEnabled;
    private readonly int _forwardEmployeeId;
    private readonly DateTime _now;

    public UpdateConversationsSettingsFlow(
        ConversationsSettings settings, 
        bool awayMessageEnabled, 
        DateTime? awayMessageEnabledFrom, 
        DateTime? awayMessageEnabledTo, 
        ConversationAwayMessageTemplate? awayMessageTemplate, 
        bool forwardEmployeeEnabled, 
        int forwardEmployeeId, 
        DateTime now)
    {
        _settings = settings;
        _awayMessageEnabled = awayMessageEnabled;
        _awayMessageEnabledFrom = awayMessageEnabledFrom;
        _awayMessageEnabledTo = awayMessageEnabledTo;
        _awayMessageTemplate = awayMessageTemplate;
        _forwardEmployeeEnabled = forwardEmployeeEnabled;
        _forwardEmployeeId = forwardEmployeeId;
        _now = now;
    }

    public MaterialisableFlowResult Execute()
    {
        var previousForwardEmployeeId = _settings.ForwardEmployeeId;

        // Checking to see if forward employee has changed to remove the prior employee from conversations
        var shouldRemoveDelegatedEmployee = ShouldRemoveDelegatedEmployee();

        var domain = new ConversationSettingsDomain(_settings);

        if (_awayMessageEnabled)
        {
            if (_awayMessageTemplate is null)
            {
                throw new DomainException("Away message template does not exist");
            }
            
            domain.EnableAwayMessage(
                enabledFrom: _awayMessageEnabledFrom,
                enabledTo: _awayMessageEnabledTo,
                template: _awayMessageTemplate
            );
        }
        else
        {
            domain.DisableAwayMessage();
        }

        if (_forwardEmployeeEnabled)
        {
            domain.EnableMessageForwarding(
                forwardTo: _forwardEmployeeId, 
                now: _now
            );
        }
        else
        {
            domain.DisableMessageForwarding();
        }

        return _settings.Updated() + RiseEvent(previousForwardEmployeeId, shouldRemoveDelegatedEmployee);
    }

    /// <summary>
    /// Checks if delegated employee should be removed from conversations
    /// </summary>
    private bool ShouldRemoveDelegatedEmployee()
    {
        switch (_forwardEmployeeEnabled)
        {
            // this is case when forwarding was turned off
            case false when _settings.ForwardEmployeeEnabled:

            // this is case when forwarding employee was turned changed
            case true when _forwardEmployeeId != _settings.ForwardEmployeeId && _settings.ForwardEmployeeId > 0:
                return true;

            default:
                return false;
        }
    }

    private INotification RiseEvent(int previousForwardEmployeeId, bool shouldRemoveDelegatedEmployee)
        => new ConversationSettingsUpdatedEvent(
            EmployeeId: _settings.EmployeeId,
            PreviousForwardEmployeeId: previousForwardEmployeeId,
            ShouldRemoveDelegatedEmployee: shouldRemoveDelegatedEmployee
        );
}