using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Shortcuts;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Queries;

namespace WildHealth.Application.Services.ShortcutGroups
{
    /// <summary>
    /// <see cref="IShortcutGroupService"/>
    /// </summary>
    public class ShortcutGroupService : IShortcutGroupService
    {
        private readonly IGeneralRepository<ShortcutGroup> _shortcutGroupsRepository;

        public ShortcutGroupService(IGeneralRepository<ShortcutGroup> shortcutGroupsRepository)
        {
            _shortcutGroupsRepository = shortcutGroupsRepository;
        }

        /// <summary>
        /// <see cref="IShortcutGroupService.GetByIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="employeeId"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public Task<ShortcutGroup> GetByIdAsync(int id, int employeeId, int practiceId)
        {
            return _shortcutGroupsRepository
                .All()
                .ById(id)
                .ByEmployeeId(employeeId)
                .RelatedToPractice(practiceId)
                .IncludeShortcuts()
                .FindAsync();
        }

        /// <summary>
        /// <see cref="IShortcutGroupService.GetAsync"/>
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ShortcutGroup>> GetAsync(int employeeId, int practiceId)
        {
            var groups = await _shortcutGroupsRepository
                .All()
                .ByEmployeeId(employeeId)
                .RelatedToPractice(practiceId)
                .IncludeShortcuts()
                .AsNoTracking()
                .ToArrayAsync();

            return groups;
        }

        /// <summary>
        /// Asserts if shortcut group exists with same name for same employee
        /// </summary>
        /// <param name="name"></param>
        /// <param name="employeeId"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task AssertNameIsUnique(string name, int employeeId, int practiceId)
        {
            var groupExists = await _shortcutGroupsRepository
                .Get(x => x.Name == name)
                .RelatedToPractice(practiceId)
                .ByEmployeeId(employeeId)
                .AnyAsync();

            if (groupExists)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Shortcut group with name: {name} already exists");
            }
        }
    }
}