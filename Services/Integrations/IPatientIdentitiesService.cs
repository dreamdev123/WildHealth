
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Common.Models.Patients;

namespace WildHealth.Application.Services.Integrations
{
    /// <summary>
    /// Provides methods for returning various patient identities in other integrations
    /// </summary>
    public interface IPatientIdentitiesService
    {
        /// <summary>
        /// Returns the patient identity models for a given unique patient identity combination 
        /// </summary>
        /// <param name="vendor"></param>
        /// <param name="vendorId"></param>
        /// <returns></returns>
        Task<IEnumerable<PatientIdentityModel>> GetAsync(IntegrationVendor vendor, string vendorId);
    }
}