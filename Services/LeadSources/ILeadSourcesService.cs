using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.LeadSources;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Services.LeadSources
{
    public interface ILeadSourcesService
    {
        /// <summary>
        /// Returns lead source by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<LeadSource> GetAsync(int id, int practiceId);

        /// <summary>
        /// Returns all lead sources by practice id
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<List<LeadSource>> GetAllAsync(int practiceId);

        /// <summary>
        /// Returns all active lead sources by practice id
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<List<LeadSource>> GetActiveAsync(int practiceId);

        /// <summary>
        /// Creates new lead source
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isOther"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<LeadSource> CreateAsync(string name, bool isOther, int practiceId);

        /// <summary>
        /// Changes activity(IsActive) of lead source
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<LeadSource> ChangeActivityAsync(int id);

        /// <summary>
        /// Deletes lead source
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<LeadSource> DeleteAsync(int id);

        /// <summary>
        /// Creates patient lead source
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="leadSource"></param>
        /// <param name="otherSource"></param>
        /// <param name="podcastSource"></param>
        /// <returns></returns>
        Task<PatientLeadSource> CreatePatientLeadSourceAsync(Patient patient, LeadSource leadSource, string? otherSource = null, string? podcastSource = null);

        /// <summary>
        /// Deletes patient lead source
        /// </summary>
        /// <param name="patientLeadSource"></param>
        /// <returns></returns>
        Task<PatientLeadSource> DeletePatientLeadSourceAsync(PatientLeadSource patientLeadSource);
    }
}
