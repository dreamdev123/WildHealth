using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Models;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.InsuranceConfigurations;

public class InsurancePlanService : IInsurancePlanService
{
    private readonly IGeneralRepository<InsurancePlan> _insurancePlansRepository;

    public InsurancePlanService(IGeneralRepository<InsurancePlan> insurancePlansRepository)
    {
        _insurancePlansRepository = insurancePlansRepository;
    }

    public async Task<InsurancePlan[]> GetAsync(int insuranceId, int practiceId)
    {
        var result = await _insurancePlansRepository
            .All()
            .Where(o => o.InsuranceId == insuranceId && o.PracticeId == practiceId)
            .ToArrayAsync();
        
        return result;
    }
    
    public async Task<InsurancePlan> GetByIdAsync(int id)
    {
        var result = await _insurancePlansRepository
            .All()
            .ById(id)
            .FindAsync();

        return result;
    }
    
    public async Task<InsurancePlan> CreateAsync(InsurancePlan insurancePlan)
    {
        await _insurancePlansRepository.AddAsync(insurancePlan);

        await _insurancePlansRepository.SaveAsync();

        return insurancePlan;
    }
    
    public async Task<InsurancePlan> UpdateAsync(InsurancePlan insurancePlan)
    {
        _insurancePlansRepository.Edit(insurancePlan);

        await _insurancePlansRepository.SaveAsync();

        return insurancePlan;
    }
    
    public async Task DeleteAsync(int id)
    {
        var insurancePlan = await GetByIdAsync(id);
        
        _insurancePlansRepository.Delete(insurancePlan);

        await _insurancePlansRepository.SaveAsync();
    }
}