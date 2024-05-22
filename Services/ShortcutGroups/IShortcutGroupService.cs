using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Shortcuts;

namespace WildHealth.Application.Services.ShortcutGroups
{
    /// <summary>
    /// Provides methods for working with shortcut groups
    /// </summary>
    public interface IShortcutGroupService
    {
        /// <summary>
        /// Returns group by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="employeeId"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<ShortcutGroup> GetByIdAsync(int id, int employeeId, int practiceId);
        
        /// <summary>
        /// Returns all group related to employee
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<IEnumerable<ShortcutGroup>> GetAsync(int employeeId, int practiceId);

        /// <summary>
        /// Asserts group is unique in scope of employee and practice
        /// </summary>
        /// <param name="name"></param>
        /// <param name="employeeId"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task AssertNameIsUnique(string name, int employeeId, int practiceId);
    }
}