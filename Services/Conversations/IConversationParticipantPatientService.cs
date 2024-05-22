using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// Provides methods for working with table ConversationMessageUnreadNotification
    /// </summary>
    public interface IConversationParticipantPatientService
    {
        Task<ConversationParticipantPatient?> GetByVendorExternalIdentityAndConversationId(string vendorExternalIdentity, int conversationId);

        /// <summary>
        /// Returns all entries that do not have a vendorExternalId
        /// </summary>
        /// <returns></returns>
        Task<ConversationParticipantPatient[]> GetHealthParticipantsWithoutExternalId();
    }
}
