using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.Payments;

namespace WildHealth.Application.Services.PaymentPlans
{
    /// <summary>
    /// Manage payment plans
    /// </summary>
    public interface IPaymentPlansService
    {
        /// <summary>
        /// Checks and returns if corresponding payment plan is founder plan
        /// </summary>
        /// <param name="paymentPeriodId"></param>
        /// <param name="paymentPriceId"></param>
        /// <returns></returns>
        Task<bool> IsFounderPlanAsync(int paymentPeriodId, int paymentPriceId);
        
        /// <summary>
        /// Returns active payment plans
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="employerProduct"></param>
        /// <returns></returns>
        Task<ICollection<PaymentPlan>> GetActiveAsync(int practiceId, EmployerProduct employerProduct);

        /// <summary>
        /// Returns all payment plans with no tracking
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<ICollection<PaymentPlan>> GetAllAsync(int practiceId);
        
        /// <summary>
        /// Returns all payment available for new Promo Codes
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<ICollection<PaymentPlan>> GetForPromoCode(int practiceId);

        /// <summary>
        /// Returns active payment plan by paymentPlanId
        /// </summary>
        /// <param name="paymentPlanId"></param>
        /// <returns></returns>
        Task<PaymentPlan> GetByIdAsync(int paymentPlanId);

        /// <summary>
        /// Returns all payment plans by ids
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<ICollection<PaymentPlan>> GetByIdsAsync(int [] ids, int practiceId);

        /// <summary>
        /// Returns active payment plan by identifier
        /// </summary>
        /// <param name="paymentPlanId"></param>
        /// <param name="paymentPeriodId"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<PaymentPlan> GetActivePlanAsync(int paymentPlanId, int paymentPeriodId, int practiceId);
        
        /// <summary>
        /// Returns payment plan by identifier
        /// </summary>
        /// <param name="paymentPlanId"></param>
        /// <param name="paymentPeriodId"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<PaymentPlan> GetPlanAsync(int paymentPlanId, int paymentPeriodId, int practiceId);

        /// <summary>
        /// Find payment coupon and returns exclusive price  
        /// </summary>
        /// <param name="couponCode"></param>
        /// <param name="paymentPeriodId"></param>
        /// <param name="paymentPriceType"></param>
        /// <returns></returns>
        Task<PaymentPrice[]> GetPaymentPricesByCouponAsync(
            string couponCode, 
            int paymentPeriodId, 
            PaymentPriceType paymentPriceType);

        /// <summary>
        /// Returns coupon by payment price
        /// </summary>
        /// <param name="paymentPriceId"></param>
        /// <returns></returns>
        Task<PaymentCoupon?> GetCouponByPaymentPriceIdAsync(int paymentPriceId);

        /// <summary>
        /// Get the payment price by the recurring payment details
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="vendor"></param>
        /// <param name="purpose"></param>
        /// <param name="amountInDollars"></param>
        /// <param name="interval"></param>
        /// <param name="intervalCount"></param>
        /// <returns></returns>
        Task<PaymentPrice> GetPaymentPriceByRecurringDetails(
            string integrationId,
            IntegrationVendor vendor,
            string purpose,
            decimal amountInDollars,
            string interval,
            int intervalCount);
        
        /// <summary>
        /// Get payment price by integration id, price and payment strategy
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="vendor"></param>
        /// <param name="purpose"></param>
        /// <param name="price"></param>
        /// <param name="strategy"></param>
        /// <returns></returns>
        Task<PaymentPrice> GetPaymentPriceByIntegrationIdAsync(
            string integrationId, 
            IntegrationVendor vendor,
            string purpose,
            decimal price, 
            PaymentStrategy strategy);

        /// <summary>
        /// Get payment price by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<PaymentPrice> GetPaymentPriceByIdAsync(int id);

        /// <summary>
        /// Returns newly created payment plan
        /// </summary>
        /// <param name="paymentPlan"></param>
        /// <returns></returns>
        Task<PaymentPlan> CreatePaymentPlanAsync(PaymentPlan paymentPlan);
        
        /// <summary>
        /// Returns newly created payment price
        /// </summary>
        /// <param name="paymentPrice"></param>
        /// <returns></returns>
        Task<PaymentPrice> CreatePaymentPriceAsync(PaymentPrice paymentPrice);

        /// <summary>
        /// Get the payment price for the given parameters
        /// </summary>
        /// <param name="planName"></param>
        /// <param name="paymentStrategy"></param>
        /// <param name="isInsurance"></param>
        /// <returns></returns>
        Task<PaymentPrice> GetPriceV2(string planName, PaymentStrategy paymentStrategy, bool isInsurance);

        
        /// <summary>
        /// Returns all PaymentCoupons for payment plans
        /// </summary>
        /// <param name="paymentPlans"></param>
        /// <returns></returns>
        Task<PaymentCoupon[]> GetPaymentCouponCodesForPlans(PaymentPlan[] paymentPlans);

        /// <summary>
        /// Returns a Payment Plan by period Id.
        /// </summary>
        /// <param name="paymentPeriodId"></param>
        /// <returns></returns>
        Task<PaymentPlan?> GetByPaymentPeriodId(int paymentPeriodId);

        /// <summary>
        /// Creates paymentPlanInsuranceState
        /// </summary>
        /// <param name="paymentPlanInsuranceState"></param>
        /// <returns></returns>
        Task<PaymentPlanInsuranceState> CreatePaymentPlanInsuranceStateAsync(PaymentPlanInsuranceState paymentPlanInsuranceState);

        /// <summary>
        /// Delete an paymentPlanInsurance state by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<PaymentPlanInsuranceState> DeletePaymentPlanInsuranceStateAsync(int id);
    }
}
