using System.Threading.Tasks;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Services.Roles
{
    /// <summary>
    /// Provides methods for working with roles
    /// </summary>
    public interface IRolesService
    {
        /// <summary>
        /// Returns role by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Role> GetByIdAsync(int id);
        
        /// <summary>
        /// Returns available roles
        /// </summary>
        /// <returns></returns>
        Task<Role[]> GetAvailableAsync();
    }
}