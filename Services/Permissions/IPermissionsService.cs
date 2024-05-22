using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Users;
using WildHealth.Licensing.Api.Enums.Licenses;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Services.Permissions
{
    /// <summary>
    /// Provides methods for working with permissions
    /// </summary>
    public interface IPermissionsService
    {
        /// <summary>
        /// Returns all permissions
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Permission>> GetAllAsync();

        /// <summary>
        /// Returns all available permissions
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Permission>> GetAvailableAsync();
        
        /// <summary>
        /// Returns corresponding permissions
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<Permission[]> GetAsync(IEnumerable<int> ids);

        /// <summary>
        /// Returns user owned permissions
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="accessStatus"></param>
        /// <returns></returns>
        Task<PermissionType[]> GetOwnedPermissionsAsync(UserIdentity identity, LicenseAccessStatus accessStatus);
    }
}