using System.Threading.Tasks;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Practices;

namespace WildHealth.Application.Cloners.Patients
{
    /// <summary>
    /// Represents patient cloner
    /// </summary>
    public interface IPatientCloner
    {
        /// <summary>
        /// Clones patient
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="coreEmail"></param>
        /// <param name="toPractice"></param>
        /// <returns></returns>
        Task<Patient> ClonePatientForNewPracticeAsync(Patient patient, string coreEmail, Practice toPractice);
    }
}