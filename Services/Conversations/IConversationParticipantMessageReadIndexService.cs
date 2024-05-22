using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Conversations;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Infrastructure.Data.Queries.CustomSql.Models;

namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// Provides methods for working with table ConversationMessageUnreadNotification
    /// </summary>
    public interface IConversationParticipantMessageReadIndexService
    {
        /// <summary>
        /// Returns ConversationParticipantMessageReadIndex by participant and conversation ids
        /// </summary>
        /// <param name="conversationExternalVendorId"></param>
        /// <param name="participantExternalVendorId"></param>
        /// <returns></returns>
        Task<ConversationParticipantMessageReadIndex?> GetByConversationAndParticipantAsync(string conversationExternalVendorId, string participantExternalVendorId);

        /// <summary>
        /// Creates a ConversationParticipantMessageReadIndex record
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<ConversationParticipantMessageReadIndex> CreateAsync(ConversationParticipantMessageReadIndex model);

        /// <summary>
        /// Get read index by conversationSid and user identity
        /// </summary>
        /// <param name="conversationVendorExternalId"></param>
        /// <param name="participantVendorExternalIdentity"></param>
        /// <returns></returns>
        Task<ConversationParticipantMessageReadIndex?> GetByConversationAndParticipantIdentityAsync(
            string conversationVendorExternalId, string participantVendorExternalIdentity);

        /// <summary>
        /// Creates a ConversationParticipantMessageReadIndex record
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<ConversationParticipantMessageReadIndex> UpdateAsync(ConversationParticipantMessageReadIndex model);

        /// <summary>
        /// Creates a ConversationParticipantMessageReadIndex record
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<ConversationParticipantMessageUnreadModel>> GetUnreadConversationParticipantIndexesWithoutNotifications();
        
        /// <summary>
        /// Returns unread messages from premium patients
        /// </summary>
        /// <returns></returns>
        Task<EmployeeUnreadMessageFromParticularPatientsModel[]> GetUnreadMessagesFromParticularPatientAsync();
    }
}
