using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Insurances;

namespace WildHealth.Application.Services.Insurances;

public interface IInsuranceVerificationService
{
    /// <summary>
    /// Creates insurance verification for patient
    /// </summary>
    /// <param name="insuranceVerification"></param>
    /// <returns></returns>
    Task<InsuranceVerification> CreateAsync(InsuranceVerification insuranceVerification);

    /// <summary>
    /// Return all insurance verifications for patient
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    Task<ICollection<InsuranceVerification>> GetAllForPatient(int patientId);

    /// <summary>
    /// Update insurance verification
    /// </summary>
    /// <param name="insuranceVerification"></param>
    /// <returns></returns>
    Task<InsuranceVerification> UpdateAsync(InsuranceVerification insuranceVerification);

    /// <summary>
    /// Returns insurance verification by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<InsuranceVerification> GetByIdAsync(int id);
}