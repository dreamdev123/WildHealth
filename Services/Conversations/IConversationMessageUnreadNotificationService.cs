using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// Provides methods for working with table ConversationMessageUnreadNotification
    /// </summary>
    public interface IConversationMessageUnreadNotificationService
    {
        /// <summary>
        /// Returns all ConversationMessageUnreadNotification by UserId and ConversationId
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        Task<IEnumerable<ConversationMessageUnreadNotification>> GetByUserConversationAsync(int userId,int conversationId);

        /// <summary>
        /// Returns last ConversationMessageUnreadNotification by UserId and ConversationId
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        Task<ConversationMessageUnreadNotification> GetLastByUserConversationAsync(int userId,int conversationId);

        /// <summary>
        /// Creates notification
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        Task<ConversationMessageUnreadNotification> CreateAsync(ConversationMessageUnreadNotification notification);

        /// <summary>
        /// Updates notification
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        Task<ConversationMessageUnreadNotification> UpdateAsync(ConversationMessageUnreadNotification notification);

        /// <summary>
        /// Gets notifications where action purpose has not been completed yet
        /// </summary>
        ///  <param name="conversationExternalVendorId"></param>
        /// <param name="participantExternalVendorId"></param>
        /// <returns></returns>
        Task<IEnumerable<ConversationMessageUnreadNotification>> GetOutstandingNotificationsAsync(string conversationExternalVendorId, string participantExternalVendorId);
    }
}
