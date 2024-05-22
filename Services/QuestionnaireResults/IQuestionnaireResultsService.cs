using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Questionnaires;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.Domain.Enums.Questionnaires;

namespace WildHealth.Application.Services.QuestionnaireResults
{
    /// <summary>
    /// Provides methods for working with health questionnaire results
    /// </summary>
    public interface IQuestionnaireResultsService
    {
        /// <summary>
        /// Starts questionnaire
        /// </summary>
        /// <param name="questionnaireId"></param>
        /// <param name="patientId"></param>
        /// <param name="sequenceNumber"></param>
        /// <returns></returns>
        Task<QuestionnaireResult> StartAsync(int questionnaireId, int patientId, int? sequenceNumber, int? appointmentId);


        /// <summary>
        /// Get all results with answers for patient
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<IEnumerable<QuestionnaireResult>> GetForPatient(int patientId);

        /// <summary>
        /// Returns Patients Health Questionnaire result by intake identifier
        /// </summary>
        /// <param name="id"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<QuestionnaireResult> GetAsync(int id, int patientId);
        
        /// <summary>
        /// Returns Patients Health Questionnaire results by intake identifier
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<IEnumerable<QuestionnaireResult>> GetAllAsync(int patientId);
        
        /// <summary>
        /// Returns Patients latest Health Questionnaire results by questionnaire types
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        Task<IEnumerable<QuestionnaireResult>> GetLatestAsync(int patientId, QuestionnaireType[] types);

        /// <summary>
        /// Returns Patients latest Health Forms results
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<IEnumerable<QuestionnaireResult>> GetLatestHealthFormsAsync(int patientId);

        /// <summary>
        /// Saves answers for the questionnaire
        /// </summary>
        /// <param name="questionnaire"></param>
        /// <param name="answers"></param>
        /// <returns></returns>
        Task<QuestionnaireResult> SaveAnswersAsync(QuestionnaireResult questionnaire, IEnumerable<AnswerModel> answers);

        /// <summary>
        /// Set submitted status for the questionnaire
        /// </summary>
        /// <param name="questionnaire"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        Task<QuestionnaireResult> SubmitAsync(QuestionnaireResult questionnaire, DateTime dateTime);

        /// <summary>
        /// Remove questionnaire
        /// </summary>
        /// <param name="questionnaireResult"></param>
        /// <returns></returns>
        Task RemoveAsync(QuestionnaireResult questionnaireResult);

        /// <summary>
        /// Clean up expired follow up forms 
        /// </summary>
        /// <returns></returns>
        Task RemoveExpiredAsync();
        
        /// <summary>
        /// <see cref="IQuestionnaireResultsService.GetGoalsAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<QuestionnaireResult> GetGoalsAsync(int patientId);


        /// <summary>
        /// <see cref="IQuestionnaireResultsService.GetDetailedAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<QuestionnaireResult> GetDetailedAsync(int patientId);


        /// <summary>
        /// <see cref="IQuestionnaireResultsService.GetMedicalAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<QuestionnaireResult> GetMedicalAsync(int patientId);
        
        
        /// <summary>
        /// Returns follow up form by appointment id
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <returns></returns>
        Task<int?> GetFollowUpFormIdAsync(int appointmentId);
        
        
        /// <summary>
        /// Returns follow up forms by appointment ids
        /// </summary>
        /// <param name="appointmentIds"></param>
        /// <returns></returns>
        Task<QuestionnaireResult[]> GetFollowUpFormsByIdsAsync(int[] appointmentIds);
    }
}
