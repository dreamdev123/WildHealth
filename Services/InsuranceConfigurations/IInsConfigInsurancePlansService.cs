using System.Threading.Tasks;
using WildHealth.Domain.Entities.InsuranceConfigurations;

namespace WildHealth.Application.Services.InsuranceConfigurations;

public interface IInsConfigInsurancePlansService
{
    Task<InsConfigInsurancePlan> GetByIdAsync(int id);

    Task<InsConfigInsurancePlan> CreateAsync(InsConfigInsurancePlan insurancePlanConfig);

    Task<InsConfigInsurancePlan> UpdateAsync(InsConfigInsurancePlan insurancePlanConfig);

    Task DeleteAsync(int id);

    Task<InsConfigInsurancePlan[]> GetByServiceConfigurationIdAsync(int serviceConfigurationId);
}