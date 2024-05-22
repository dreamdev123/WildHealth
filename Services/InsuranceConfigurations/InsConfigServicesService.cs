using System.Net;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.InsuranceConfigurations;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.InsuranceConfigurations;

public class InsConfigServicesService : IInsConfigServicesService
{
    private readonly IGeneralRepository<InsConfigService> _serviceConfigurationsRepository;

    public InsConfigServicesService(IGeneralRepository<InsConfigService> serviceConfigurationsRepository)
    {
        _serviceConfigurationsRepository = serviceConfigurationsRepository;
    }

    public async Task<InsConfigService> GetByIdAsync(int id)
    {
        var result = await _serviceConfigurationsRepository
            .All()
            .ById(id)
            .Include(o => o.InsurancePlanConfigs)
            .ThenInclude(o => o.InsurancePlan)
            .Include(o => o.ServiceStateConfigs)
            .ThenInclude(o => o.State)
            .FirstOrDefaultAsync();
            
        if (result is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Service configuration with id = {id} does not exist.");
        }
        
        return result;
    }
    
    public async Task<InsConfigService> CreateAsync(InsConfigService serviceConfiguration)
    {
        await _serviceConfigurationsRepository.AddAsync(serviceConfiguration);

        await _serviceConfigurationsRepository.SaveAsync();

        return serviceConfiguration;
    }

    public async Task<InsConfigService> UpdateAsync(InsConfigService serviceConfiguration)
    {
        _serviceConfigurationsRepository.Edit(serviceConfiguration);

        await _serviceConfigurationsRepository.SaveAsync();

        return serviceConfiguration;
    }
    
    public async Task DeleteAsync(int id)
    {
        var serviceConfiguration = await GetByIdAsync(id);
        
        _serviceConfigurationsRepository.Delete(serviceConfiguration);

        await _serviceConfigurationsRepository.SaveAsync();
    }
}