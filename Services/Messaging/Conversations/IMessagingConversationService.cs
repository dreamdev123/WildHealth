using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.Models.ConversationParticipants;

namespace WildHealth.Application.Services.Messaging.Conversations
{
    public interface IMessagingConversationService
    {
        /// <summary>
        /// Creates conversation
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="conversation"></param>
        /// <returns></returns>
        Task<ConversationModel> CreateConversationAsync(int practiceId, Conversation conversation);

        /// <summary>
        /// Creates conversation participant
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="conversation"></param>
        /// <param name="messagingIdentity"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<ConversationParticipantResponseModel> CreateConversationParticipantAsync(
            int practiceId,
            Conversation conversation,
            string messagingIdentity,
            string name);

        /// <summary>
        /// Deletes conversation participant
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="conversation"></param>
        /// <param name="vendorExternalId"></param>
        /// <returns></returns>
        Task RemoveConversationParticipantAsync(
            int practiceId,
            Conversation conversation,
            string vendorExternalId);
    }
}

