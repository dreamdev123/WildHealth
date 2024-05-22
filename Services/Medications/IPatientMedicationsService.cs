using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Medications;
using WildHealth.Domain.Entities.Medication;

namespace WildHealth.Application.Services.Medications
{
    /// <summary>
    /// Provides methods for working with patient medications
    /// </summary>
    public interface IPatientMedicationsService
    {
        /// <summary>
        /// Returns patient medications by patient id
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<IEnumerable<PatientMedication>> GetAsync(int patientId);

        /// <summary>
        /// Returns patient medications by ids
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<PatientMedication[]> GetByIdsAsync(int[] ids);

        /// <summary>
        /// Creates patient medication
        /// </summary>
        /// <param name="model"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<PatientMedication> CreateAsync(CreatePatientMedicationModel model, int patientId);

        /// <summary>
        /// Updates patient medication
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<PatientMedication> EditAsync(PatientMedicationModel model);

        /// <summary>
        /// Deletes patient medication
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<PatientMedication> DeleteAsync(int id);
    }
}
