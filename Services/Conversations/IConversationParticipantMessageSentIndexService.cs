using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// Provides methods for working with table ConversationMessageUnreadNotification
    /// </summary>
    public interface IConversationParticipantMessageSentIndexService
    {
        /// <summary>
        /// Get a ConversationParticipantMessageSentIndex if exists
        /// </summary>
        /// <param name="conversationVendorExternalId"></param>
        /// <param name="participantVendorExternalId"></param>
        /// <returns>ConversationParticipantMessageSentIndex</returns>
        Task<ConversationParticipantMessageSentIndex?> GetByConversationAndParticipantAsync(
            string conversationVendorExternalId, string participantVendorExternalId);
    }
}
