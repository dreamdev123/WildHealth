using System;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    public class UpdateConversationsMessageSentIndexCommand : IRequest
    {
        public int ConversationId { get; }
        public string ConversationVendorExternalId { get; }
        public string ParticipantVendorExternalId { get; }
        public Guid ParticipantIdentity { get; }
        public int Index { get; }
        public DateTime CreatedAt { get; }

        public UpdateConversationsMessageSentIndexCommand(
            int conversationId,
            string conversationVendorExternalId,
            string participantVendorExternalId,
            int index,
            DateTime createdAt,
            Guid participantIdentity)
        {
            ConversationId = conversationId;
            ConversationVendorExternalId = conversationVendorExternalId;
            ParticipantVendorExternalId = participantVendorExternalId;
            CreatedAt = createdAt;
            Index = index;
            ParticipantIdentity = participantIdentity;
        }
    }
}