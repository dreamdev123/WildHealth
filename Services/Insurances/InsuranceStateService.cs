using System.Threading.Tasks;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Insurances;

public class InsuranceStateService : IInsuranceStateService
{
    private readonly IGeneralRepository<InsuranceState> _insuranceStateRepository;

    public InsuranceStateService(IGeneralRepository<InsuranceState> insuranceStateRepository)
    {
        _insuranceStateRepository = insuranceStateRepository;
    }

    public async Task<InsuranceState> CreateAsync(InsuranceState insuranceState)
    {
        await _insuranceStateRepository.AddAsync(insuranceState);
        
        await _insuranceStateRepository.SaveAsync();

        return insuranceState;
    }
    
    public async Task<InsuranceState> DeleteAsync(int id)
    {
        var insuranceState = await _insuranceStateRepository.GetAsync(id);
        
        _insuranceStateRepository.Delete(insuranceState);

        await _insuranceStateRepository.SaveAsync();

        return insuranceState;
    }
}