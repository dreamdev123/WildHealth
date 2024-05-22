using System.Threading.Tasks;
using WildHealth.Domain.Entities.Insurances;

namespace WildHealth.Application.Services.InsuranceConfigurations;

public interface IInsurancePlanService
{
    Task<InsurancePlan[]> GetAsync(int insuranceId, int practiceId);

    Task<InsurancePlan> CreateAsync(InsurancePlan insurancePlan);

    Task<InsurancePlan> UpdateAsync(InsurancePlan insurancePlan);

    Task DeleteAsync(int id);
}