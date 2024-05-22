using System.Threading.Tasks;
using WildHealth.Domain.Entities.Insurances;

namespace WildHealth.Application.Services.Insurances;

public interface IInsuranceStateService
{
    /// <summary>
    /// Creates insurance state
    /// </summary>
    /// <param name="insuranceState"></param>
    /// <returns></returns>
    Task<InsuranceState> CreateAsync(InsuranceState insuranceState);

    /// <summary>
    /// Delete an insurance state by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<InsuranceState> DeleteAsync(int id);
}