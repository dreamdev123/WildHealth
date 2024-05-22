using System.Threading.Tasks;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Enums.Integrations;

namespace WildHealth.Application.Services.Insurances;

public interface IInsuranceService
{
    /// <summary>
    /// Creates insurance
    /// </summary>
    /// <param name="insurance"></param>
    /// <returns></returns>
    Task<Insurance> CreateAsync(Insurance insurance);

    /// <summary>
    /// Returns all insurance
    /// </summary>
    /// <returns></returns>
    Task<Insurance[]> GetAllAsync();


    /// <summary>
    /// Returns insurance by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Insurance?> GetByIdAsync(int id);

    /// <summary>
    /// Returns all insurance for a state
    /// </summary>
    /// <param name="stateId"></param>
    /// <param name="age"></param>
    /// <returns></returns>
    Task<Insurance[]> GetByStateAsync(int stateId, int? age);

    /// <summary>
    /// Returns insurance by integration id
    /// </summary>
    /// <param name="integrationId"></param>
    /// <param name="vendor"></param>
    /// <returns></returns>
    Task<Insurance?> GetByIntegrationIdAsync(string integrationId, IntegrationVendor vendor);
    
    /// <summary>
    /// Returns insurance by name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    Task<Insurance> GetByNameAsync(string name);
}