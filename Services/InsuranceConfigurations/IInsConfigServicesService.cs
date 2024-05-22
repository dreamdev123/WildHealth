using System.Threading.Tasks;
using WildHealth.Domain.Entities.InsuranceConfigurations;

namespace WildHealth.Application.Services.InsuranceConfigurations;

public interface IInsConfigServicesService
{
    /// <summary>
    /// Get service configuration by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<InsConfigService> GetByIdAsync(int id);

    /// <summary>
    /// Create a new service configuration
    /// </summary>
    /// <param name="serviceConfiguration"></param>
    /// <returns></returns>
    Task<InsConfigService> CreateAsync(InsConfigService serviceConfiguration);

    /// <summary>
    /// Update an existing service configuration
    /// </summary>
    /// <param name="serviceConfiguration"></param>
    /// <returns></returns>
    Task<InsConfigService> UpdateAsync(InsConfigService serviceConfiguration);

    /// <summary>
    /// Delete service configuration by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task DeleteAsync(int id);
}