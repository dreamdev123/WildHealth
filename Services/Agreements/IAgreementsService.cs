using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Agreements;
using WildHealth.Domain.Entities.Agreements;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Services.Agreements
{
    /// <summary>
    /// Provides method for agreements
    /// </summary>
    public interface IAgreementsService
    {
        /// <summary>
        /// for remove
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<AgreementConfirmation>> GetAgreementConfirmationRange(int from, int to);

        /// <summary>
        /// Returns patient unsigned confirmations
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<IEnumerable<AgreementConfirmation>> GetUnsignedConfirmationsAsync(int patientId);
        
        /// <summary>
        /// Returns changed agreements owned by patient
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<IEnumerable<Agreement>> GetChangedAgreementsAsync(int patientId);

        /// <summary>
        /// Returns patient agreement confirmation with document by id and patient id
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<AgreementConfirmation> GetPatientConfirmationWithDocumentAsync(int patientId, int id);

        /// <summary>
        /// Returns patient agreement confirmation with included document by id and intakeId
        /// </summary>
        /// <param name="intakeId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<AgreementConfirmation> GetPatientConfirmationWithDocumentAsync(Guid intakeId, int id);

        /// <summary>
        /// Returns patient agreements confirmations by intake id
        /// </summary>
        /// <param name="intakeId"></param>
        /// <returns></returns>
        Task<AgreementConfirmation[]> GetPatientConfirmationsAsync(Guid intakeId);
        
        /// <summary>
        /// Returns patient agreements confirmations by patient identifier
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<AgreementConfirmation[]> GetPatientConfirmationsAsync(int patientId);
        
        /// <summary>
        /// Creates empty agreement confirmations
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        Task<IEnumerable<AgreementConfirmation>> CreateUnsignedConfirmationsAsync(
            Patient patient, 
            Subscription subscription);

        /// <summary>
        /// Confirms patient agreements
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="subscription"></param>
        /// <param name="models"></param>
        /// <returns></returns>
        Task<AgreementConfirmation[]> ConfirmAgreementsAsync(Patient patient,
            Subscription subscription,
            ConfirmAgreementModel[]? models);
        
        /// <summary>
        /// Sign patient agreement
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="confirmation"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<AgreementConfirmation> SignAgreementAsync(
            Patient patient,
            AgreementConfirmation confirmation,
            ConfirmAgreementModel model);

        /// <summary>
        /// Copy previous agreements for new subscription
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="oldSubscription"></param>
        /// <param name="newSubscription"></param>
        /// <returns></returns>
        Task<AgreementConfirmation[]> CopyAgreementsAsync(
            Patient patient, 
            Subscription oldSubscription, 
            Subscription newSubscription);

        /// <summary>
        /// Generates and returns agreement confirmation file name
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="agreementName"></param>
        /// <returns></returns>
        string GenerateFileName(int patientId, string agreementName);
    }
}