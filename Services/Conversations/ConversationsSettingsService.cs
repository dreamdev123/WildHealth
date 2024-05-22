using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// <see cref="IConversationsSettingsService"/>
    /// </summary>
    public class ConversationsSettingsService : IConversationsSettingsService
    {
        private readonly IGeneralRepository<ConversationsSettings> _conversationsSettingsRepository;

        public ConversationsSettingsService(IGeneralRepository<ConversationsSettings> conversationsSettingsRepository)
        {
            _conversationsSettingsRepository = conversationsSettingsRepository;
        }

        /// <summary>
        /// <see cref="IConversationsSettingsService.GetByEmployeeIdLegacy"/>
        /// </summary>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<ConversationsSettings> GetByEmployeeIdLegacy(int employeeId)
        {
            var settings = await _conversationsSettingsRepository
                .Get(x => x.EmployeeId == employeeId)
                .IncludeAwayMessageTemplate()
                .FirstOrDefaultAsync();

            if (settings is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(employeeId), employeeId);
                throw new AppException(HttpStatusCode.NotFound, "Conversations Settings for employee do not exist.", exceptionParam);
            }

            return settings;
        }

        /// <summary>
        /// <see cref="IConversationsSettingsService.GetPageAsync"/>
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="searchQuery"></param>
        /// <param name="sortingDirection"></param>
        /// <returns></returns>
        public async Task<(int, ICollection<ConversationsSettings>)> GetPageAsync(int page, int pageSize, string? searchQuery, string sortingDirection)
        {
            var allConversationsSettings = _conversationsSettingsRepository
                .All()
                .ActiveForward()
                .Include(x => x.Employee)
                .ThenInclude(x => x.User)
                .IncludeAwayMessageTemplate()
                .SortByDirection(sortingDirection)
                .BySearchQuery(searchQuery);
            
            var totalCount = await allConversationsSettings.CountAsync();
            var settingsResult = await allConversationsSettings.Pagination(page * pageSize, pageSize).ToArrayAsync();

            return (totalCount, settingsResult);
        }

        /// <summary>
        /// <see cref="IConversationsSettingsService.GetByEmployeeIdsAsync"/>
        /// </summary>
        /// <param name="employeeIds"></param>
        /// <returns></returns>
        public async Task<ConversationsSettings[]> GetByEmployeeIdsAsync(int[] employeeIds)
        {
            var settings = await _conversationsSettingsRepository
                .All()
                .IncludeAwayMessageTemplate()
                .Where(x => employeeIds.Contains(x.EmployeeId))
                .ToArrayAsync();

            return settings;
        }

        /// <summary>
        /// <see cref="IConversationsSettingsService.CreateConversationsSettingsAsync"/>
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public async Task<ConversationsSettings> CreateConversationsSettingsAsync(ConversationsSettings settings)
        {
            await _conversationsSettingsRepository.AddAsync(settings);

            await _conversationsSettingsRepository.SaveAsync();

            return settings;
        }
    }
}
