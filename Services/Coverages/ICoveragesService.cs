using System.Threading.Tasks;
using WildHealth.Domain.Entities.Insurances;

namespace WildHealth.Application.Services.Coverages;

/// <summary>
/// Provides methods for working with coverages
/// </summary>
public interface ICoveragesService
{
    /// <summary>
    /// Returns coverage
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Coverage> GetAsync(int id);
    
    /// <summary>
    /// Returns primary coverage related to user
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<Coverage[]> GetPrimaryAsync(int userId);
    
    /// <summary>
    /// returns all coverages
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<Coverage[]> GetAllAsync(int userId);
    
    /// <summary>
    /// Creates and returns coverage
    /// </summary>
    /// <param name="coverage"></param>
    /// <returns></returns>
    Task<Coverage> CreateAsync(Coverage coverage);

    /// <summary>
    /// Updates and returns coverage
    /// </summary>
    /// <param name="coverage"></param>
    /// <returns></returns>
    Task<Coverage> UpdateAsync(Coverage coverage);
}