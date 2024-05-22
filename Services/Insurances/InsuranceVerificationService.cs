using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Insurances;

public class InsuranceVerificationService : IInsuranceVerificationService
{
    private readonly IGeneralRepository<InsuranceVerification> _insuranceVerificationRepository;

    public InsuranceVerificationService(IGeneralRepository<InsuranceVerification> insuranceVerificationRepository)
    {
        _insuranceVerificationRepository = insuranceVerificationRepository;
    }
    
    /// <summary>
    /// <see cref="IInsuranceVerificationService.CreateAsync(InsuranceVerification)"/>
    /// </summary>
    /// <param name="insuranceVerification"></param>
    /// <returns></returns>
    public async Task<InsuranceVerification> CreateAsync(InsuranceVerification insuranceVerification)
    {
        await _insuranceVerificationRepository.AddAsync(insuranceVerification);

        await _insuranceVerificationRepository.SaveAsync();

        return insuranceVerification;
    }

    /// <summary>
    /// <see cref="IInsuranceVerificationService.GetAllForPatient(int)"/>
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    public async Task<ICollection<InsuranceVerification>> GetAllForPatient(int patientId)
    {
        var result = await _insuranceVerificationRepository
            .All()
            .RelatedToPatient(patientId)
            .ToArrayAsync();
        
        return result;
    }

    /// <summary>
    /// <see cref="IInsuranceVerificationService.UpdateAsync(InsuranceVerification)"/>
    /// </summary>
    /// <param name="insuranceVerification"></param>
    /// <returns></returns>
    public async Task<InsuranceVerification> UpdateAsync(InsuranceVerification insuranceVerification)
    {
        _insuranceVerificationRepository.Edit(insuranceVerification);
        await _insuranceVerificationRepository.SaveAsync();
        return insuranceVerification;
    }

    /// <summary>
    /// <see cref="IInsuranceVerificationService.GetByIdAsync"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<InsuranceVerification> GetByIdAsync(int id)
    {
        var result = await _insuranceVerificationRepository
            .All()
            .ById(id)
            .FindAsync();

        return result;
    }
}