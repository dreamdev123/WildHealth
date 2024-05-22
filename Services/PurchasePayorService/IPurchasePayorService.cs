using System;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Interfaces;

namespace WildHealth.Application.Services.PurchasePayorService
{
    /// <summary>
    /// Provides methods for application purchasePayor
    /// </summary>
    public interface IPurchasePayorService
    {
        /// <summary>
        /// Creates a PurchasePayor with the given parameters
        /// </summary>
        /// <param name="payable"></param>
        /// <param name="payor"></param>
        /// <param name="patient"></param>
        /// <param name="amount"></param>
        /// <param name="billableOnDate"></param>
        /// <param name="isBilled"></param>
        /// <returns></returns>
        Task<PurchasePayor> CreateAsync(IPayable payable,
            IPayor? payor,
            Patient patient,
            decimal amount,
            DateTime? billableOnDate,
            bool isBilled = false);
        
        /// <summary>
        /// Returns purchase payor records related to payable
        /// </summary>
        /// <param name="payableUniversalId"></param>
        /// <returns></returns>
        Task<PurchasePayor[]> SelectAsync(Guid payableUniversalId);
    }
}

