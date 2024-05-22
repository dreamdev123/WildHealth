using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// Provides methods for working with conversation settings
    /// </summary>
    public interface IConversationsSettingsService
    {
        /// <summary>
        /// Gets conversations settings for employee.
        /// </summary>
        /// <param name="employeeId">Target employee.</param>
        /// <returns>Conversations settings of employee.</returns>
        Task<ConversationsSettings> GetByEmployeeIdLegacy(int employeeId);

        /// <summary>
        /// Return forwarded messages 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="searchQuery"></param>
        /// <param name="sortingDirection"></param>
        /// <returns></returns>
        Task<(int, ICollection<ConversationsSettings>)> GetPageAsync(int page, int pageSize, string? searchQuery, string sortingDirection);
        
        /// <summary>
        /// Returns conversation settings by employee ids
        /// </summary>
        /// <param name="employeeIds">Target employee.</param>
        /// <returns>Conversations settings of employee.</returns>
        Task<ConversationsSettings[]> GetByEmployeeIdsAsync(int[] employeeIds);

        /// <summary>
        /// Create conversations settings for employee.
        /// </summary>
        /// <param name="settings">Target settings.</param>
        /// <returns>>Conversations settings of employee.</returns>
        Task<ConversationsSettings> CreateConversationsSettingsAsync(ConversationsSettings settings);
    }
}
