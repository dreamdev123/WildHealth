using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.CommandHandlers.Conversations.Flows;

public class BumpMessageSentIndexFlow : IMaterialisableFlow
{
    private readonly ConversationParticipantMessageSentIndex? _messageSentIndex;
    private readonly int _conversationId;
    private readonly string _conversationVendorExternalId;
    private readonly string _participantVendorExternalId;
    private readonly DateTime _messageSentAt;
    private readonly int _index;
    private readonly Guid _participantUniversalId;

    public BumpMessageSentIndexFlow(
        ConversationParticipantMessageSentIndex? messageSentIndex,
        int conversationId,
        string conversationVendorExternalId,
        string participantVendorExternalId,
        DateTime messageSentAt,
        int index,
        Guid participantUniversalId)
    {
        _messageSentIndex = messageSentIndex;
        _conversationId = conversationId;
        _conversationVendorExternalId = conversationVendorExternalId;
        _participantVendorExternalId = participantVendorExternalId;
        _messageSentAt = messageSentAt;
        _index = index;
        _participantUniversalId = participantUniversalId;
    }

    public MaterialisableFlowResult Execute()
    {
        if (_participantUniversalId == Guid.Empty)
            return MaterialisableFlowResult.Empty;
        
        if (_messageSentIndex is null)
        {
            var newEntity = ConversationParticipantMessageSentIndex.Create(
                conversationId: _conversationId,
                conversationVendorExternalId: _conversationVendorExternalId,
                participantVendorExternalId: _participantVendorExternalId,
                lastMessageSentDate: _messageSentAt,
                lastMessageSentIndex: _index,
                participantIdentity: _participantUniversalId);

            return newEntity.Added().ToFlowResult();
        }
        
        _messageSentIndex.SetLastMessageSentDate(_messageSentAt);
        _messageSentIndex.SetLastMessageSentIndex(_index);

        return _messageSentIndex.Updated().ToFlowResult();
    }
}