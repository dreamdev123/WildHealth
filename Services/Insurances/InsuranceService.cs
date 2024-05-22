using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Constants;
using WildHealth.Shared.Data.Repository;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Insurances;

public class InsuranceService : IInsuranceService
{
    private readonly IGeneralRepository<Insurance> _insuranceRepository;

    public InsuranceService(IGeneralRepository<Insurance> insuranceRepository)
    {
        _insuranceRepository = insuranceRepository;
    }

    /// <summary>
    /// <see cref="IInsuranceService.CreateAsync"/>
    /// </summary>
    /// <param name="insurance"></param>
    /// <returns></returns>
    public async Task<Insurance> CreateAsync(Insurance insurance)
    {
        await _insuranceRepository.AddAsync(insurance);
    
        await _insuranceRepository.SaveAsync();
    
        return insurance;
    }

    /// <summary>
    /// <see cref="IInsuranceService.GetAllAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<Insurance[]> GetAllAsync()
    {
        var result = await _insuranceRepository
            .All()
            .IncludeStates()
            .IncludeIntegrations<Insurance, InsuranceIntegration>()
            .ToArrayAsync();
    
        return result;
    }
    
    /// <summary>
    /// <see cref="IInsuranceService.GetByIdAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<Insurance?> GetByIdAsync(int id)
    {
        var result = await _insuranceRepository
            .All()
            .ById(id)
            .IncludeStates()
            .IncludeIntegrations<Insurance, InsuranceIntegration>()
            .FirstOrDefaultAsync();
    
        return result;
    }
    
    /// <summary>
    /// <see cref="IInsuranceService.GetByNameAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<Insurance> GetByNameAsync(string name)
    {
        var result = await _insuranceRepository
            .All()
            .Where(o => o.Name == name)
            .IncludeStates()
            .IncludeIntegrations<Insurance, InsuranceIntegration>()
            .FirstAsync();
    
        return result;
    }

    /// <summary>
    /// <see cref="IInsuranceService.GetByStateAsync"/>
    /// </summary>
    /// <param name="stateId"></param>
    /// <param name="age"></param>
    /// <returns></returns>
    public async Task<Insurance[]> GetByStateAsync(int stateId, int? age)
    {
        var result = await _insuranceRepository
            .All()
            .ByAge(age)
            .ByStateId(stateId)
            .IncludeStates()
            .IncludeIntegrations<Insurance, InsuranceIntegration>()
            .ToArrayAsync();
    
        return result;
    }

    /// <summary>
    /// <see cref="IInsuranceService.GetByIntegrationIdAsync"/>
    /// </summary>
    /// <param name="integrationId"></param>
    /// <param name="vendor"></param>
    /// <returns></returns>
    /// <exception cref="AppException"></exception>
    public async Task<Insurance?> GetByIntegrationIdAsync(string integrationId, IntegrationVendor vendor)
    {
        var insurance = await _insuranceRepository
            .All()
            .ByIntegrationId<Insurance, InsuranceIntegration>(integrationId, vendor,
                IntegrationPurposes.Insurance.ExternalId)
            .FirstOrDefaultAsync();

        return insurance;
    }
}