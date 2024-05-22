using System.Threading.Tasks;
using WildHealth.Domain.Entities.InsuranceConfigurations;

namespace WildHealth.Application.Services.InsuranceConfigurations;

public interface IInsuranceConfigsService
{
    /// <summary>
    /// Get insurance configuration by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<InsuranceConfig> GetByIdAsync(int id);

    /// <summary>
    /// Get insurance configurations by practice id
    /// </summary>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    Task<InsuranceConfig[]> GetByPracticeIdAsync(int practiceId);

    /// <summary>
    /// Get insurance configuration by practice id and insurance id
    /// </summary>
    /// <param name="practiceId"></param>
    /// <param name="insuranceId"></param>
    /// <returns></returns>
    Task<InsuranceConfig[]> GetAsync(int practiceId, int insuranceId);

    /// <summary>
    /// Create a new insurance configuration
    /// </summary>
    /// <param name="insuranceConfiguration"></param>
    /// <returns></returns>
    Task<InsuranceConfig> CreateAsync(InsuranceConfig insuranceConfiguration);

    /// <summary>
    /// Update an insurance configuration
    /// </summary>
    /// <param name="insuranceConfiguration"></param>
    /// <returns></returns>
    Task<InsuranceConfig> UpdateAsync(InsuranceConfig insuranceConfiguration);
    
    /// <summary>
    /// Delete an insurance configuration
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task DeleteAsync(int id);
}