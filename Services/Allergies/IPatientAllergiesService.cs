using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Alergies;
using WildHealth.Domain.Entities.Alergies;

namespace WildHealth.Application.Services.Allergies
{
    /// <summary>
    /// Provides methods for working with patient allergies
    /// </summary>
    public interface IPatientAllergiesService
    {
        /// <summary>
        /// Returns patient allergies by patient id
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<IEnumerable<PatientAlergy>> GetByPatientIdAsync(int patientId);

        /// <summary>
        /// Returns patient allergies by ids
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<PatientAlergy[]> GetByIdsAsync(int[] ids);

        /// <summary>
        /// Creates patient allergies
        /// </summary>
        /// <param name="model"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<PatientAlergy> CreatePatientAllergyAsync(CreatePatientAlergyModel model, int patientId);

        /// <summary>
        /// Updates patient allergy
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<PatientAlergy> EditPatientAllergyAsync(PatientAlergyModel model);

        /// <summary>
        /// Deletes patient allergies
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<PatientAlergy> DeleteAsync(int id);
    }
}
