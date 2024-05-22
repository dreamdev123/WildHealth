using System.Threading.Tasks;
using WildHealth.Domain.Entities.InsuranceConfigurations;

namespace WildHealth.Application.Services.InsuranceConfigurations;

public interface IInsConfigServiceStatesService
{
    Task<InsConfigServiceState> GetByIdAsync(int id);

    Task<InsConfigServiceState> CreateAsync(InsConfigServiceState serviceStateConfiguration);

    Task<InsConfigServiceState> UpdateAsync(InsConfigServiceState serviceStateConfiguration);

    Task DeleteAsync(int id);

    Task<InsConfigServiceState[]> GetByServiceConfigurationIdAsync(int serviceConfigurationId);
}