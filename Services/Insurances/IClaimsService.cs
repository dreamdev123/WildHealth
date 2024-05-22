using System.Threading.Tasks;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Enums.Integrations;

namespace WildHealth.Application.Services.Insurances;

public interface IClaimsService
{
    /// <summary>
    /// Create claim
    /// </summary>
    /// <param name="claim"></param>
    /// <returns></returns>
    Task<Claim> CreateAsync(Claim claim);

    /// <summary>
    /// Update claim
    /// </summary>
    /// <param name="claim"></param>
    /// <returns></returns>
    Task<Claim> UpdateAsync(Claim claim);

    /// <summary>
    /// Get claim by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Claim> GetById(int id);

    /// <summary>
    /// Get claim by integration id
    /// </summary>
    /// <param name="integrationId"></param>
    /// <param name="vendor"></param>
    /// <param name="purpose"></param>
    /// <returns></returns>
    Task<Claim?> GetByIntegrationIdAsync(string integrationId, IntegrationVendor vendor, string purpose);
}