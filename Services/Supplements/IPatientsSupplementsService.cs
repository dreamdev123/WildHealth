using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Supplement;
using WildHealth.Domain.Entities.Notes.Common;
using WildHealth.Common.Models.Supplements;

namespace WildHealth.Application.Services.Supplements
{
    /// <summary>
    /// Provides methods for working with patient supplements
    /// </summary>
    public interface IPatientsSupplementsService
    {
        /// <summary>
        /// Returns patient supplements by patient id
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<IEnumerable<PatientSupplement>> GetAsync(int patientId);

        /// <summary>
        /// Returns default supplement link for practice
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<string?> GetDefaultLinkAsync(int practiceId);

        /// <summary>
        /// Returns supplement by id
        /// </summary>
        /// <param name="supplementId"></param>
        /// <returns></returns>
        Task<PatientSupplement> GetByIdAsync(int supplementId);

        /// <summary>
        /// Returns supplements by ids
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<PatientSupplement[]> GetByIdsAsync(int[] ids);
    
        /// <summary>
        /// Returns Common supplement by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<CommonSupplement> GetCommonSupplementByIdAsync(int id);

        /// <summary>
        /// Returns all Common supplements
        /// </summary>
        /// <returns></returns>
        Task<CommonSupplement[]> GetCommonSupplementsAsync();

        /// <summary>
        /// creates common supplement
        /// <param name="model"></param>
        /// </summary>
        /// <returns></returns>
        Task<CommonSupplement> CreateCommonSupplementAsync(CommonSupplementModel model);

        /// <summary>
        /// Returns an updated Common supplement
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<CommonSupplement> UpdateCommonSupplementAsync(CommonSupplementModel model);

        /// <summary>
        /// Deletes Common supplement
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<CommonSupplement> DeleteCommonSupplementAsync(int id);
    }
}
