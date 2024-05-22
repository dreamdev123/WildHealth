using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Services.Locations
{
    public interface ILocationsService
    {
        /// <summary>
        /// Returns default location
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<Location> GetDefaultLocationAsync(int practiceId);

        /// <summary>
        /// Returns fellowship location
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<Location> GetFellowshipLocationAsync(int practiceId);

        /// <summary>
        /// Returns location by identifier
        /// </summary>
        /// <param name="id"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<Location> GetByIdAsync(int id, int practiceId);

        /// <summary>
        /// Returns locations by identifier
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<Location[]> GetByIdsAsync(ICollection<int> ids, int practiceId);

        /// <summary>
        /// Returns all locations
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<IEnumerable<Location>> GetAllAsync(int practiceId);

        /// <summary>
        /// Returns own location ids
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        Task<int[]> GetOwnedLocationIdsAsync(UserIdentity identity);

        /// <summary>
        /// Creates location
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        Task<Location> CreateAsync(Location location);

        /// <summary>
        /// Updates location
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        Task<Location> UpdateAsync(Location location);
    }
}
