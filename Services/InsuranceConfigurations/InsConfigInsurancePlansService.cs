using System.Linq;
using System.Net;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.InsuranceConfigurations;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.InsuranceConfigurations;

public class InsConfigInsurancePlansService : IInsConfigInsurancePlansService
{
    private readonly IGeneralRepository<InsConfigInsurancePlan> _insConfigInsurancePlansRepository;

    public InsConfigInsurancePlansService(IGeneralRepository<InsConfigInsurancePlan> insConfigInsurancePlansRepository)
    {
        _insConfigInsurancePlansRepository = insConfigInsurancePlansRepository;
    }

    public async Task<InsConfigInsurancePlan> GetByIdAsync(int id)
    {
        var result = await _insConfigInsurancePlansRepository
            .All()
            .ById(id)
            .FirstOrDefaultAsync();
            
        if (result is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Service state configuration with id = {id} does not exist.");
        }
        
        return result;
    }
    
    public async Task<InsConfigInsurancePlan[]> GetByServiceConfigurationIdAsync(int serviceConfigurationId)
    {
        var results = await _insConfigInsurancePlansRepository
            .All()
            .Where(o => o.ServiceConfigId == serviceConfigurationId)
            .ToArrayAsync();

        return results;
    }
    
    public async Task<InsConfigInsurancePlan> CreateAsync(InsConfigInsurancePlan insurancePlanConfig)
    {
        await _insConfigInsurancePlansRepository.AddAsync(insurancePlanConfig);

        await _insConfigInsurancePlansRepository.SaveAsync();

        return insurancePlanConfig;
    }

    public async Task<InsConfigInsurancePlan> UpdateAsync(InsConfigInsurancePlan serviceStateConfiguration)
    {
        _insConfigInsurancePlansRepository.Edit(serviceStateConfiguration);

        await _insConfigInsurancePlansRepository.SaveAsync();

        return serviceStateConfiguration;
    }
    
    public async Task DeleteAsync(int id)
    {
        var serviceStateConfiguration = await GetByIdAsync(id);
        
        _insConfigInsurancePlansRepository.Delete(serviceStateConfiguration);

        await _insConfigInsurancePlansRepository.SaveAsync();
    }
}