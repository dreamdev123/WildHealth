using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Enums.AddOns;
using WildHealth.Domain.Enums.User;
using WildHealth.Common.Models.AddOns;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Orders;

namespace WildHealth.Application.Services.AddOns
{
    /// <summary>
    /// Provides methods for working with Add-Ons
    /// </summary>
    public interface IAddOnsService
    {
        /// <summary>
        /// Returns active add-ons available for ordering
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="gender"></param>
        /// <param name="orderType"></param>
        /// <param name="employerKey"></param>
        /// <param name="grouping"></param>
        /// <returns></returns>
        Task<IEnumerable<AddOn>> GetAvailableForOrderingAsync(
            int practiceId,
            Gender gender,
            OrderType? orderType = null,
            string? employerKey = null,
            bool grouping = false);

        /// <summary>
        /// Selects and Returns active add-ons available for ordering
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="gender"></param>
        /// <param name="provider"></param>
        /// <param name="employerKey"></param>
        /// <param name="searchQuery"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        Task<(IEnumerable<AddOn>, int)> SelectAvailableForOrderingAsync(int practiceId,
            Gender gender,
            AddOnProvider provider,
            string? employerKey,
            string? searchQuery,
            int? skip = 0,
            int? take = 50);

        /// <summary>
        /// Returns active and optional add-ons for payment plan
        /// </summary>
        /// <param name="paymentPlanId"></param>
        /// <param name="gender"></param>
        ///  <param name="employerKey"></param>
        /// <returns></returns>
        Task<IEnumerable<AddOn>> GetOptionalAsync(int paymentPlanId, Gender gender, string? employerKey);

        /// <summary>
        /// Returns active and required add-ons for payment plan
        /// </summary>
        /// <param name="paymentPlanId"></param>
        /// <param name="gender"></param>
        /// <param name="employerKey"></param>
        /// <returns></returns>
        Task<IEnumerable<AddOn>> GetRequiredAsync(int paymentPlanId, Gender gender, string? employerKey);

        /// <summary>
        /// Returns active add-ons by integration ids
        /// </summary>
        /// <param name="integrationIds"></param>
        /// <param name="employerKey"></param>
        /// <returns></returns>
        Task<IEnumerable<AddOn>> GetByIntegrationIdsAsync(string[] integrationIds, string? employerKey);

        /// <summary>
        /// Returns add-ons by ids
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="practiceId"></param>
        /// <param name="employerKey"></param>
        /// <returns></returns>
        Task<IEnumerable<AddOn>> GetByIdsAsync(IEnumerable<int> ids, int practiceId, string? employerKey = default);

        /// <summary>
        /// Creates add-on
        /// </summary>
        /// <param name="addOn"></param>
        /// <returns></returns>
        Task<AddOn> CreateAddOnAsync(AddOn addOn);

        /// <summary>
        /// Returns all add-ons by a specified provider in a form of a dictionary with IntegrationId as key
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        Task<Dictionary<string, AddOn>> GetByProviderAsync(AddOnProvider provider);

        /// <summary>
        /// Updates an existing AddOn
        /// </summary>
        /// <param name="addOn"></param>
        /// <returns></returns>
        Task UpdateAsync(AddOn addOn);

        /// <summary>
        /// Returns add-on by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<AddOn> GetByIdAsync(int id);

        /// <summary>
        /// Returns an updated AddOn
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<AddOn> EditAsync(AddOnModel model);

        /// <summary>
        /// Deletes add-on
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<AddOn> DeleteAsync(int id);

        /// <summary>
        /// Takes in a collection of AddOns and locates the common parent for the given patient
        /// </summary>
        /// <param name="addOn"></param>
        /// <param name="patient"></param>
        /// <returns></returns>
        Task<AddOn?> GetParentForAddOn(AddOn addOn, Patient patient);
    }
}