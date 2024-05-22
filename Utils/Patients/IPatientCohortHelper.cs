using System.Threading.Tasks;
using WildHealth.Common.Enums.Patients;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Utils.Patients;

public interface IPatientCohortHelper
{
    /// <summary>
    /// Get a list of all cohorts that a patient belongs to
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    Task<PatientCohort[]> GetCohortsForPatientId(int patientId);

    /// <summary>
    /// Returns result of whether the patient is in the given cohorts, logical AND
    /// </summary>
    /// <param name="patientId"></param>
    /// <param name="cohorts"></param>
    /// <returns></returns>
    Task<bool> PatientInAllCohorts(int patientId, PatientCohort[] cohorts);

    /// <summary>
    /// Returns result of whether the patient is in the given cohorts, logical OR
    /// </summary>
    /// <param name="patientId"></param>
    /// <param name="cohorts"></param>
    /// <returns></returns>
    Task<bool> PatientInAnyCohorts(int patientId, PatientCohort[] cohorts);
    
    #region convenience

    Task<bool> IsPremiumPatient(int patientId);

    #endregion
}