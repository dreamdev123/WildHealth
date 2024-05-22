using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Shortcuts;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Shortcuts
{
    public class ShortcutsService: IShortcutsService
    {
        private readonly IGeneralRepository<Shortcut> _shortcutsRepository;

        public ShortcutsService(IGeneralRepository<Shortcut> shortcutsRepository)
        {
            _shortcutsRepository = shortcutsRepository;
        }

        public Task<Shortcut> GetAsync(int id, int employeeId, int practiceId)
        {
            return _shortcutsRepository
                .All()
                .ById(id)
                .ByEmployeeId(employeeId)
                .RelatedToPractice(practiceId)
                .IncludeGroup()
                .FindAsync();
        }
        
        /// <summary>
        /// <see cref="IShortcutsService.AssertNameIsUniqueAsync"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="employeeId"></param>
        /// <param name="practiceId"></param>
        /// <exception cref="AppException"></exception>
        public async Task AssertNameIsUniqueAsync(string name, int employeeId, int practiceId)
        {
            var shortcutExists = await _shortcutsRepository
                .Get(x => x.Name == name)
                .ByEmployeeId(employeeId)
                .RelatedToPractice(practiceId)
                .AnyAsync();

            if (shortcutExists)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Shortcut with name: {name} already exists");
            }
        }
    }
}