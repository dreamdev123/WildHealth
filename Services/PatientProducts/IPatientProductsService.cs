using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Products;

namespace WildHealth.Application.Services.PatientProducts
{
    /// <summary>
    /// Manages patient products
    /// </summary>
    public interface IPatientProductsService
    {
        /// <summary>
        /// Returns patient product by identifier
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<PatientProduct> GetAsync(int id);

        /// <summary>
        /// Returns all active patient products
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<PatientProduct[]> GetActiveAsync(int patientId);

        /// <summary>
        /// Returns patient product by type
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="type"></param>
        /// <param name="builtInSourceId"></param>
        /// <returns></returns>
        Task<PatientProduct?> GetByTypeAsync(int patientId, ProductType type, Guid builtInSourceId);

        /// <summary>
        /// Get by sourceId and includes additional
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="builtInSourceId"></param>
        /// <returns></returns>
        Task<PatientProduct?> GetBySourceIdAndAdditionalAsync(int patientId, Guid builtInSourceId);

        /// <summary>
        /// Selects patient products by
        ///     * Payment Status
        ///     * Payment Flow
        ///     * Used from date
        ///     * Used to date
        /// </summary>
        /// <param name="paymentStatuses"></param>
        /// <param name="paymentFlow"></param>
        /// <param name="usedFrom"></param>
        /// <param name="usedTo"></param>
        /// <param name="patientId"></param>
        /// <param name="productTypes"></param>
        /// <returns></returns>
        Task<PatientProduct[]> SelectAsync(ProductPaymentStatus[] paymentStatuses,
            PaymentFlow? paymentFlow,
            DateTime? usedFrom,
            DateTime? usedTo,
            int? patientId = null,
            ProductType[]? productTypes = null);

        /// <summary>
        /// Returns all patient products related to this subscription or are purchased
        /// additionally outside of a subscription that persist perpetually
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="currentSubscription"></param>
        /// <returns></returns>
        Task<PatientProduct[]> GetBySubscriptionAsync(int patientId, Subscription currentSubscription);

        /// <summary>
        /// Get all by patient and type
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="productType"></param>
        /// <param name="productSubType"></param>
        /// <returns></returns>
        Task<PatientProduct[]> GetByPatientIdAndProductTypeAsync(
            int patientId, 
            ProductType productType, 
            ProductSubType productSubType);
        
        /// <summary>
        /// Creates patient products
        /// </summary>
        /// <param name="patientProducts"></param>
        /// <returns></returns>
        Task<PatientProduct[]> CreateAsync(PatientProduct[] patientProducts);

        /// <summary>
        /// Updates patient products
        /// </summary>
        /// <param name="patientProducts"></param>
        /// <returns></returns>
        Task<PatientProduct[]> UpdateAsync(PatientProduct[] patientProducts);
        
        /// <summary>
        /// Updates patient products
        /// </summary>
        /// <param name="patientProduct"></param>
        /// <returns></returns>
        Task<PatientProduct> UpdateAsync(PatientProduct patientProduct);
        
        /// <summary>
        /// Uses patient product
        /// </summary>
        /// <param name="id"></param>
        /// <param name="usedBy"></param>
        /// <param name="usedAt"></param>
        /// <returns></returns>
        Task UseAsync(int id, string usedBy, DateTime usedAt);

        /// <summary>
        /// Use a collection of patient products
        /// </summary>
        /// <param name="patientProducts"></param>
        /// <param name="usedBy"></param>
        /// <param name="usedAt"></param>
        /// <returns></returns>
        Task UseBulkAsync(IEnumerable<PatientProduct> patientProducts, string usedBy, DateTime usedAt);
        
        /// <summary>
        /// Use a collection of patient products
        /// </summary>
        /// <param name="patientProducts"></param>
        /// <param name="expiredBy"></param>
        /// <param name="expiredAt"></param>
        /// <returns></returns>
        Task ExpireBulkAsync(IEnumerable<PatientProduct> patientProducts, string expiredBy, DateTime expiredAt);

        /// <summary>
        /// Returns active built in products by patientId
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<ICollection<PatientProduct>> GetBuiltInByPatientAsync(int patientId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<PatientProduct[]> GetBuiltInProductsForCurrentSubscription(int patientId);

        /// <summary>
        /// Subscription Products
        /// </summary>
        /// <param name="subscriptionUniversalId"></param>
        /// <returns></returns>
        Task<PatientProduct[]> GetSubscriptionProductsAsync(Guid subscriptionUniversalId);

        /// <summary>
        /// Deletes patient product
        /// </summary>
        /// <param name="patientProduct"></param>
        /// <returns></returns>
        Task<PatientProduct> DeleteAsync(PatientProduct patientProduct);
        
        /// <summary>
        /// Deletes patient products
        /// </summary>
        /// <param name="patientProducts"></param>
        /// <returns></returns>
        Task<PatientProduct[]> DeleteAsync(PatientProduct[] patientProducts);
    }
}

