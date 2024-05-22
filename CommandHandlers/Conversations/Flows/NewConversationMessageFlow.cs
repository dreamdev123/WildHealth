using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using WildHealth.Application.Domain.PatientEngagements;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Domain.Models.Timeline;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Conversations.Flows;

public class NewConversationMessageFlow : IMaterialisableFlow
{
    private readonly Conversation _conversation;
    private readonly List<ConversationParticipantEmployee> _employeeParticipants;
    private readonly List<ConversationParticipantPatient> _patientParticipants;
    private readonly int _senderId;
    private readonly string _messageBody;
    private readonly int _messageIndex;
    private readonly List<PatientEngagement> _conversationEngagements;
    private readonly DateTime _utcNow;
    private const int DescriptionMaxLength = 100;
    
    public NewConversationMessageFlow(Conversation conversation,
        List<ConversationParticipantEmployee> employeeParticipants,
        List<ConversationParticipantPatient> patientParticipants,
        int senderId,
        string messageBody,
        int messageIndex,
        List<PatientEngagement> conversationEngagements,
        DateTime utcNow)
    {
        _conversation = conversation;
        _employeeParticipants = employeeParticipants;
        _patientParticipants = patientParticipants;
        _senderId = senderId;
        _messageBody = messageBody;
        _messageIndex = messageIndex;
        _conversationEngagements = conversationEngagements;
        _utcNow = utcNow;
    }

    public MaterialisableFlowResult Execute()
    {
        UpdateConversationLastMessageTime();

        var domain = new ConversationDomain(_conversation);
        
        domain.SetIndex(_messageIndex);
        
        if (!IsConversationStarted())
            domain.ComposeDescription(_messageBody, DescriptionMaxLength);

        return _conversation.Updated() + TimelineEvent().Added() + CompleteConversationEngagements();
    }

    private PatientTimelineEvent? TimelineEvent()
    {
        // Only internal messages between employees should be showing on the timeline.
        if (_conversation.Type != ConversationType.Internal) return null;

        var allParticipants = _employeeParticipants.Select(x => x.Employee.User)
            .Concat(_patientParticipants.Select(x => x.Patient.User));

        var senderUser = allParticipants.FirstOrDefault(x => x.Id == _senderId);
        var patient = _patientParticipants.FirstOrDefault()?.Patient; // can be at most one patient in the conversation
        var allRecipients = allParticipants.Where(x => x.Id != _senderId);

        if (senderUser is null || patient is null || senderUser == patient.User) return null;
        
        var from = new MessageSentTimelineEvent.Participant(senderUser.FirstName, senderUser.LastName, senderUser.Employee?.Credentials);
        var to = allRecipients
            .Where(x => x.Id != patient?.UserId)
            .Prepend(patient!.User) // current user should go first in the recipients list
            .Select(x => new MessageSentTimelineEvent.Participant(x.FirstName, x.LastName, x.Employee?.Credentials))
            .ToArray();
            
        if (patient is not null)
            return new MessageSentTimelineEvent(patient.GetId(), _utcNow, new MessageSentTimelineEvent.Data(from, to, _messageBody));

        return null;
    }
    
    private MaterialisableFlowResult CompleteConversationEngagements()
    {
        var isPatientSender = _patientParticipants.Any(x => x.Patient.User.Id == _senderId);
        if (!isPatientSender)
            return MaterialisableFlowResult.Empty;

        return _conversationEngagements
            .Where(e => e.NotExpired(_utcNow))
            .Select(engagement => engagement
                .With(x => x.Status = PatientEngagementStatus.Completed)
                .With(x => x.CompletedBy = 0) // 0 stands for System
                .Updated())
            .ToFlowResult();
    }

    private bool IsConversationStarted()
    {
        return _employeeParticipants.Count + _patientParticipants.Count > 1;
    }

    private void UpdateConversationLastMessageTime()
    {
        if (_conversation is null)
        {
            throw new AppException(HttpStatusCode.NotFound, "The conversation cannot be null."); 
        }
            
        _conversation.SetLastMessageTime(_utcNow);
    }
}