using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Questionnaires;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.Domain.Enums.Questionnaires;

namespace WildHealth.Application.Services.Questionnaires
{
    /// <summary>
    /// Provides methods for working with health questionnaire
    /// </summary>
    public interface IQuestionnairesService
    {
        /// <summary>
        /// Returns all questionnaires
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Questionnaire>> GetRemindedAsync();

        /// <summary>
        /// Returns if questionnaire available for corresponding patient
        /// </summary>
        /// <param name="id"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<bool> IsAvailableAsync(int id, int patientId);

        /// <summary>
        /// Returns if questionnaire available for corresponding patient
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<bool> AnyAvailableAsync(int patientId);

        /// <summary>
        /// Gets all new appointment questionnaires that became available to the patients in the last 30 minutes
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<AppointmentQuestionnaire>> GetNewAppointmentQuestionnairesAsync();
        
        /// <summary>
        /// Returns available Questionnaires
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<IEnumerable<AppointmentQuestionnaire>> GetAvailableAsync(int patientId);
        
        /// <summary>
        /// Returns active Health Questionnaire by type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        Task<Questionnaire> GetByTypeAsync(QuestionnaireType type);

        /// <summary>
        /// Returns Questionnaire by sub type
        /// </summary>
        /// <param name="subType"></param>
        /// <returns></returns>
        Task<Questionnaire> GetBySubTypeAsync(QuestionnaireSubType subType);

        /// <summary>
        /// Returns active Health Questionnaire by identifier
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Questionnaire> GetByIdAsync(int id);

        /// <summary>
        /// Returns available Health Forms for patient
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<HealthFormsModel> GetAvailableHealthFormsAsync(int patientId);
    }
}
