using System.Threading.Tasks;
using WildHealth.Common.Models.Shortcuts;
using WildHealth.Domain.Entities.Shortcuts;

namespace WildHealth.Application.Services.Shortcuts
{
    /// <summary>
    /// Provides methods for working with shortcuts
    /// </summary>
    public interface IShortcutsService
    {
        /// <summary>
        /// Returns shortcut
        /// </summary>
        /// <param name="id"></param>
        /// <param name="employeeId"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<Shortcut> GetAsync(int id, int employeeId, int practiceId);

        /// <summary>
        /// Asserts shortcut name is Unique
        /// </summary>
        /// <param name="name"></param>
        /// <param name="employeeId"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task AssertNameIsUniqueAsync(string name, int employeeId, int practiceId);
    }
}