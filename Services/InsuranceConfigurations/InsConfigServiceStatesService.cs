using System.Linq;
using System.Net;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.InsuranceConfigurations;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.InsuranceConfigurations;

public class InsConfigServiceStatesService : IInsConfigServiceStatesService
{
    private readonly IGeneralRepository<InsConfigServiceState> _serviceStateConfigurationsRepository;

    public InsConfigServiceStatesService(IGeneralRepository<InsConfigServiceState> serviceStateConfigurationsRepository)
    {
        _serviceStateConfigurationsRepository = serviceStateConfigurationsRepository;
    }

    public async Task<InsConfigServiceState> GetByIdAsync(int id)
    {
        var result = await _serviceStateConfigurationsRepository
            .All()
            .ById(id)
            .FirstOrDefaultAsync();
            
        if (result is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Service state configuration with id = {id} does not exist.");
        }
        
        return result;
    }
    
    public async Task<InsConfigServiceState[]> GetByServiceConfigurationIdAsync(int serviceConfigurationId)
    {
        var results = await _serviceStateConfigurationsRepository
            .All()
            .Where(o => o.ServiceConfigurationId == serviceConfigurationId)
            .ToArrayAsync();

        return results;
    }
    
    public async Task<InsConfigServiceState> CreateAsync(InsConfigServiceState serviceStateConfiguration)
    {
        await _serviceStateConfigurationsRepository.AddAsync(serviceStateConfiguration);

        await _serviceStateConfigurationsRepository.SaveAsync();

        return serviceStateConfiguration;
    }

    public async Task<InsConfigServiceState> UpdateAsync(InsConfigServiceState serviceStateConfiguration)
    {
        _serviceStateConfigurationsRepository.Edit(serviceStateConfiguration);

        await _serviceStateConfigurationsRepository.SaveAsync();

        return serviceStateConfiguration;
    }
    
    public async Task DeleteAsync(int id)
    {
        var serviceStateConfiguration = await GetByIdAsync(id);
        
        _serviceStateConfigurationsRepository.Delete(serviceStateConfiguration);

        await _serviceStateConfigurationsRepository.SaveAsync();
    }
}