using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.InsuranceConfigurations;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.InsuranceConfigurations;

public class InsuranceConfigsService : IInsuranceConfigsService
{
    private readonly IGeneralRepository<InsuranceConfig> _insuranceConfigurationsRepository;

    public InsuranceConfigsService(IGeneralRepository<InsuranceConfig> insuranceConfigurationsRepository)
    {
        _insuranceConfigurationsRepository = insuranceConfigurationsRepository;
    }

    public async Task<InsuranceConfig> GetByIdAsync(int id)
    {
        var result = await _insuranceConfigurationsRepository
            .All()
            .ById(id)
            .Include(o => o.ServiceConfigurations)
                .ThenInclude(o => o.ServiceStateConfigs)
                .ThenInclude(o => o.State)
            .Include(o => o.Insurance)
            .FirstOrDefaultAsync();
        
        
        if (result is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Insurance configuration with id = {id} does not exist.");
        }
        
        return result;
    }

    public async Task<InsuranceConfig[]> GetByPracticeIdAsync(int practiceId)
    {
        var result = await _insuranceConfigurationsRepository
            .All()
            .Where(o => o.PracticeId == practiceId)
            .Include(o => o.ServiceConfigurations)
                .ThenInclude(o => o.ServiceStateConfigs)
                .ThenInclude(o => o.State)
            .Include(o => o.Insurance)
            .ToArrayAsync();

        return result;
    }
    
    public async Task<InsuranceConfig[]> GetAsync(int practiceId, int insuranceId)
    {
        var result = await _insuranceConfigurationsRepository
            .All()
            .Where(o => o.PracticeId == practiceId && o.InsuranceId == insuranceId)
            .Include(o => o.ServiceConfigurations)
            .ThenInclude(o => o.ServiceStateConfigs)
            .ThenInclude(o => o.State)
            .Include(o => o.Insurance)
            .ToArrayAsync();

        return result;
    }

    public async Task<InsuranceConfig> CreateAsync(InsuranceConfig insuranceConfiguration)
    {
        await _insuranceConfigurationsRepository.AddAsync(insuranceConfiguration);

        await _insuranceConfigurationsRepository.SaveAsync();

        return insuranceConfiguration;
    }

    public async Task<InsuranceConfig> UpdateAsync(InsuranceConfig insuranceConfiguration)
    {
        _insuranceConfigurationsRepository.Edit(insuranceConfiguration);

        await _insuranceConfigurationsRepository.SaveAsync();

        return insuranceConfiguration;
    }

    public async Task DeleteAsync(int id)
    {
        var insuranceConfiguration = await GetByIdAsync(id);
        
        _insuranceConfigurationsRepository.Delete(insuranceConfiguration);

        await _insuranceConfigurationsRepository.SaveAsync();
    }
}