using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Utils.PatientCreator
{
    /// <summary>
    /// Provides mapper between test codes and lab names
    /// </summary>
    public interface IPatientCreator
    {
        /// <summary>
        /// Returns a new user model based on the parameters
        /// </summary>
        /// <param name="user"></param>
        /// <param name="patientOptions"></param>
        /// <param name="location"></param>
        /// <param name="dataTemplate"></param>
        /// <returns></returns>
        Task<Patient> Create(User user, PatientOptions? patientOptions, Location location, IDictionary<string, string>? dataTemplate = null);

        Task<Patient> AddPatientInputsAggregator(User user, Patient patient, Location location, IDictionary<string, string>? dataTemplate = null);

        /// <summary>
        ///  Catch up the data related to the Inputs Aggregator - used for existing patients that didn't get certain LabInput values when initially loaded
        /// </summary>
        /// <param name="user"></param>
        /// <param name="patient"></param>
        /// <param name="location"></param>
        /// <param name="dataTemplate"></param>
        /// <returns></returns>
        Task<Patient> CatchUpPatientInputsAggregator(User user, Patient patient, Location location, IDictionary<string, string>? dataTemplate = null);
    }
}