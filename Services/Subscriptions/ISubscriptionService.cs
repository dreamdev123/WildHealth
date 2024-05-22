using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Shared.Data.Helpers;

namespace WildHealth.Application.Services.Subscriptions
{
    /// <summary>
    /// Provides method for working with subscriptions
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>
        /// Creates patient subscription for past
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="paymentPrice"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        Task<Subscription> CreatePastSubscriptionAsync(
            Patient patient,
            PaymentPrice paymentPrice,
            DateTime startDate,
            DateTime endDate);
        
        /// <summary>
        /// Updates subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        Task<Subscription> UpdateSubscriptionAsync(Subscription subscription);

        /// <summary>
        /// Schedules subscription cancellation
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="cancellationType"></param>
        /// <param name="cancellationReason"></param>
        /// <param name="cancellationDate"></param>
        /// <returns></returns>
        Task<Subscription> ScheduleCancellationAsync(
            Subscription subscription, 
            CancellationReasonType cancellationType,
            string cancellationReason, 
            DateTime cancellationDate);

        /// <summary>
        /// Get current patient subscription
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<Subscription> GetCurrentSubscriptionAsync(int patientId);
        
        /// <summary>
        /// Get all patient subscriptions
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<Subscription[]> GetAllAsync(int patientId);

        /// <summary>
        /// Returns by identifier
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Subscription> GetAsync(int id);

        /// <summary>
        /// Returns by identifier
        /// </summary>
        /// <param name="id"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        Task<Subscription> GetAsync(int id, ISpecification<Subscription> specification);

        /// <summary>
        /// Get subscriptions that end today
        /// <param name="date"></param>
        /// <param name="practiceId"></param>
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Subscription>> GetFinishingSubscriptionsAsync(DateTime date, int practiceId);

        /// <summary>
        /// Get subscriptions that end on date range
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="practiceId"></param>
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Subscription>> GetFinishingSubscriptionsAsync(DateTime from, DateTime to, int practiceId);

        /// <summary>
        /// Get subscription by cancellation request
        /// </summary>
        /// <param name="date"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<IEnumerable<Subscription>> GetSubscriptionsToCancelAsync(DateTime date, int practiceId);

        /// <summary>
        /// Returns the best subscription candidate for a patient for editing/renewal
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<Subscription> SubscriptionCandidateForModificationAsync(int patientId);

        /// <summary>
        /// Returns subscription by integration id
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="vendor"></param>
        /// <returns></returns>
        Task<Subscription> GetByIntegrationIdAsync(string integrationId, IntegrationVendor vendor, bool activeOnly = true);

        Task<Subscription> GetByPaymentIssueId(int paymentIssueId);
    }
}
