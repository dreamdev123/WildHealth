using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Extensions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.ConversationMessages;
using WildHealth.IntegrationEvents.ConversationMessages.Payloads;

namespace WildHealth.Application.CommandHandlers.Conversations.Flows;

public class NotifyParticipantsFlow : IMaterialisableFlow
{
    private readonly Conversation _conversation;
    private readonly int[] _receiverUserIds;
    private readonly int _senderId;
    private readonly Guid _senderUniversalId;
    private readonly string _messageBody;
    private readonly string[] _fileNamesAttached;
    private readonly IEnumerable<ConversationParticipantEmployee> _employeeParticipants;
    private readonly IEnumerable<ConversationParticipantPatient> _patientParticipants;
    private const int NotificationMessageLength = 50;
    
    public NotifyParticipantsFlow(Conversation conversation,
        IEnumerable<ConversationParticipantEmployee> employeeParticipants,
        IEnumerable<ConversationParticipantPatient> patientParticipants,
        string messageBody,
        string[] fileNamesAttached,
        int[] receiverUserIds,
        int senderId, 
        Guid senderUniversalId)
    {
        _conversation = conversation;
        _employeeParticipants = employeeParticipants;
        _patientParticipants = patientParticipants;
        _messageBody = messageBody;
        _fileNamesAttached = fileNamesAttached;
        _receiverUserIds = receiverUserIds;
        _senderId = senderId;
        _senderUniversalId = senderUniversalId;
    }

    public MaterialisableFlowResult Execute()
    {
        return MobileNotification() + ClarityNotification() + SenderMessageReadEvent();
    }

    /// <summary>
    /// Integration services sends mobile notifications once the event fired.
    /// </summary>
    private MaterialisableFlowResult MobileNotification()
    {
        var notificationMessage = BuildMobileNotificationMessage();
        var deviceTokens = GetDeviceTokens();

        if (deviceTokens.Empty())
            return MaterialisableFlowResult.Empty;

        var payload = new ConversationMessageProcessedPayload(_conversation.Subject, notificationMessage, deviceTokens);
        return new ConversationMessageProcessedIntegrationEvent(payload, DateTime.UtcNow).ToFlowResult();
    }

    private MaterialisableFlowResult ClarityNotification()
    {
        var employeesUsers = _employeeParticipants.Select(x => x.Employee.User);
        var patientsUsers = _patientParticipants.Select(x => x.Patient.User);
        var allUsers = employeesUsers.Concat(patientsUsers).ToArray();
            
        var receivers = allUsers
            .Where(x => x.GetId() != _senderId)
            .Where(x => _receiverUserIds.Contains(x.GetId()))
            .ToArray();

        return receivers.Any() ? 
            new NewMessageOnConversationClarityNotification(receivers, _conversation).ToFlowResult() : 
            MaterialisableFlowResult.Empty;
    }
    
    /// <summary>
    /// Also make sure the sender of the message is marked as having read this message
    /// </summary>
    private MaterialisableFlowResult SenderMessageReadEvent()
    {
        return new ConversationMessageIntegrationEvent(
            payload: new ConversationMessageReadPayload(
                conversationId: _conversation.GetId(),
                conversationExternalVendorId: _conversation.VendorExternalId,
                participantExternalVendorId: _senderUniversalId.ToString(),
                lastMessageReadIndex: _conversation.Index.ToString()
            ),
            user: new UserMetadataModel(_senderUniversalId.ToString()),
            eventDate: DateTime.UtcNow).ToFlowResult();
    }
    
    private string[] GetDeviceTokens()
    {
        var employees = _employeeParticipants
            .Where(p => !string.IsNullOrEmpty(p.VendorExternalId)) // ignore those who are not in Twilio conversation for some reason
            .Select(y => y.Employee.User);

        var patients = _patientParticipants
            .Where(p => !string.IsNullOrEmpty(p.VendorExternalId)) // ignore those who are not in Twilio conversation for some reason (e.g internal conversation between employees)
            .Select(y => y.Patient.User);

        return employees.Concat(patients)
            .Where(x => x.GetId() != _senderId)
            .SelectMany(d => d.Devices)
            .Select(d => d.DeviceToken)
            .ToArray();
    }

    private string BuildMobileNotificationMessage()
    {
        // if message text is empty but there are files attached we take file name as a notification text 
        var body = string.IsNullOrEmpty(_messageBody) ? 
            string.Join(", ", _fileNamesAttached) : _messageBody;
            
        const string ellipses = "...";
        return body.Length <= NotificationMessageLength ?
            body : $"{body.Substring(0, NotificationMessageLength - ellipses.Length)}{ellipses}";
    }
}