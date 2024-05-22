using System.Threading.Tasks;
using WildHealth.Domain.Entities.Employees;

namespace WildHealth.Application.Services.Rosters;

/// <summary>
/// Provides methods fro working with rosters
/// </summary>
public interface IRostersService
{
    /// <summary>
    /// Returns all rosters
    /// </summary>
    /// <param name="isActive"></param>
    /// <returns></returns>
    Task<Roster[]> SelectAsync(bool? isActive = null);

    /// <summary>
    /// Returns roster by identifier
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Roster> GetAsync(int id);

    /// <summary>
    /// Creates new roster
    /// </summary>
    /// <param name="roster"></param>
    /// <returns></returns>
    Task<Roster> CreateAsync(Roster roster);
    
    /// <summary>
    /// Updates existing roster
    /// </summary>
    /// <param name="roster"></param>
    /// <returns></returns>
    Task<Roster> UpdateAsync(Roster roster);
}