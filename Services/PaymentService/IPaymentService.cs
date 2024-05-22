using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Payments;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.Products;
using WildHealth.Domain.Entities.PromoCodes;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Models.Payment;
using WildHealth.Integration.Models.Payments;
using WildHealth.Integration.Models.Subscriptions;

namespace WildHealth.Application.Services.PaymentService
{
    /// <summary>
    /// Provides methods for application payment
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Returns integration vendor async
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<IntegrationVendor> GetIntegrationVendorAsync(int practiceId);
        
        /// <summary>
        /// Returns merchant credentials
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<PaymentCredentialsModel> GetPaymentCredentialsAsync(int practiceId);

        /// <summary>
        /// Process payment subscription
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="subscriptionId"></param>
        /// <returns></returns>
        Task<PaymentIntegrationModel?> ProcessSubscriptionPaymentAsync(Patient patient, string subscriptionId);

        Task<SubscriptionIntegrationModel> BuySubscriptionAsyncV2(Patient patient,
            Subscription subscription,
            PaymentPrice paymentPrice,
            EmployerProduct? employerProduct,
            PromoCodeCoupon? promoCodeCoupon,
            bool isFirstPurchase,
            Subscription? previousSubscription = null
        );

        
        /// <summary>
        /// Creates a subscription backdated and accounts for purchase payor entries
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="subscription"></param>
        /// <param name="priorSubscription"></param>
        /// <param name="paymentPrice"></param>
        /// <param name="backdate"></param>
        /// <param name="noStartupFee"></param>
        /// <returns></returns>
        Task<SubscriptionIntegrationModel> BuySubscriptionBackdatedAsync(
            Patient patient,
            Subscription subscription,
            Subscription priorSubscription,
            PaymentPrice paymentPrice,
            DateTime backdate,
            bool noStartupFee
        );

        /// <summary>
        /// Creates a subscription backdated and accounts for purchase payroll entries
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="priorSubscription"></param>
        /// <param name="coupon"></param>
        /// <param name="patient"></param>
        /// <param name="paymentPrice"></param>
        /// <param name="employerProduct"></param>
        /// <param name="chargeStartupFee"></param>
        /// <returns></returns>
        Task<SubscriptionIntegrationModel> BuySubscriptionBackdatedAsyncV2(Subscription subscription,
            Subscription priorSubscription,
            PromoCodeCoupon? coupon,
            Patient patient,
            PaymentPrice paymentPrice,
            EmployerProduct? employerProduct,
            bool chargeStartupFee);

        /// <summary>
        /// Process add-ons payment
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="orders"></param>
        /// <param name="employerProduct"></param>
        /// <returns></returns>
        Task<PaymentIntegrationModel> ProcessOrdersPaymentAsync(Patient patient,
            Order[] orders,
            EmployerProduct? employerProduct = null);
        
        /// <summary>
        /// Process add-ons payment
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="orders"></param>
        /// <returns></returns>
        Task<PaymentIntegrationModel> ProcessFreeOrdersPaymentAsync(Patient patient, Order[] orders);

        /// <summary>
        /// Returns does add-ons support payment
        /// </summary>
        /// <param name="addOns"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<bool> CanPayForAddOnsAsync(IEnumerable<AddOn> addOns, int practiceId);
        
        /// <summary>
        /// Generates link for purchasing products
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        Task<string> GenerateBuyProductPageAsync(Patient patient, Product product);
        
        /// <summary>
        /// Disposes link for purchasing products
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="linkId"></param>
        /// <returns></returns>
        Task DisposeBuyProductPageAsync(int practiceId, string linkId);
        
        /// <summary>
        /// Generates link for purchasing products
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="invoiceId"></param>
        /// <returns></returns>
        Task<string> GenerateInvoicePageAsync(Patient patient, string invoiceId);

        /// <summary>
        /// Creates customer portal link on stripe to change billing information
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        Task<string> CreateCustomerPortalLinkAsync(Patient patient);
        
        /// <summary>
        /// Creates link to resolve customer portal on stripe to change billing information
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        Task<string> CreateResolveCustomerPortalLinkAsync(Patient patient);
        
        /// <summary>
        /// Refund the payment for an order
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        Task<RefundIntegrationModel> RefundOrderPaymentAsync(
            int patientId, 
            Order order);

        Task UpdateSubscriptionPriceAsync(int practiceId, string subscriptionId, SubscriptionPriceDomain subscriptionPrice);
    }
}
