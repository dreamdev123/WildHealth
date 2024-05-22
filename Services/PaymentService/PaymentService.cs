using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Services.AddOns;
using WildHealth.Application.Services.ConfirmCodes;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PurchasePayorService;
using WildHealth.Application.Utils.ApplyEmployerUtil;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Application.Utils.DefaultEmployerProvider;
using WildHealth.BackgroundJobs;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.Payments;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.PromoCodes;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Enums.Products;
using WildHealth.Domain.Enums.User;
using WildHealth.Domain.Interfaces;
using WildHealth.Domain.Models.Payment;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Models.Payments;
using WildHealth.Integration.Models.Subscriptions;
using Order = WildHealth.Domain.Entities.Orders.Order;
using Product = WildHealth.Domain.Entities.Products.Product;
using WildHealth.Settings;

namespace WildHealth.Application.Services.PaymentService
{
    /// <summary>
    /// <see cref="IPaymentService"/>
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private static readonly string[] PaymentCredentialsSettings =
        {
            SettingsNames.Payment.PublicKey,
            SettingsNames.Payment.Provider,
            SettingsNames.Payment.PrivateKey
        };
        
        private readonly ISettingsManager _settingsManager;
        private readonly IIntegrationServiceFactory _integrationServiceFactory;
        private readonly IPurchasePayorService _purchasePayorService;
        private readonly IEmployerProductDiscountUtil _employerProductDiscountUtil;
        private readonly IDefaultEmployerProvider _defaultEmployerProvider;
        private readonly IBackgroundJobsService _backgroundJobsService;
        private readonly IConfirmCodeService _confirmCodeService;
        private readonly IAddOnsService _addOnsService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<PaymentService> _logger;
        private readonly IPatientsService _patientsService;
        private readonly AppOptions _appOptions;
        
        public PaymentService(
            ISettingsManager settingsManager,
            IIntegrationServiceFactory integrationServiceFactory,
            IPurchasePayorService purchasePayorService,
            IEmployerProductDiscountUtil employerProductDiscountUtil,
            IDefaultEmployerProvider defaultEmployerProvider,
            IBackgroundJobsService backgroundJobsService,
            IConfirmCodeService confirmCodeService,
            IAddOnsService addOnsService,
            IDateTimeProvider dateTimeProvider,
            ILogger<PaymentService> logger,
            IPatientsService patientsService,
            IOptions<AppOptions> appOptions)
        {
            _settingsManager = settingsManager;
            _integrationServiceFactory = integrationServiceFactory;
            _purchasePayorService = purchasePayorService;
            _employerProductDiscountUtil = employerProductDiscountUtil;
            _defaultEmployerProvider = defaultEmployerProvider;
            _backgroundJobsService = backgroundJobsService;
            _confirmCodeService = confirmCodeService;
            _addOnsService = addOnsService;
            _dateTimeProvider = dateTimeProvider;
            _patientsService = patientsService;
            _appOptions = appOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// <see cref="IPaymentService.GetIntegrationVendorAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<IntegrationVendor> GetIntegrationVendorAsync(int practiceId)
        {
            var integrationService = await _integrationServiceFactory.CreateAsync(practiceId);

            return integrationService.IntegrationVendor;
        }

        /// <summary>
        /// <see cref="IPaymentService.GetPaymentCredentialsAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<PaymentCredentialsModel> GetPaymentCredentialsAsync(int practiceId)
        {
            var settings = await _settingsManager.GetSettings(PaymentCredentialsSettings, practiceId);
            
            return new PaymentCredentialsModel
            {                
                PracticeId = practiceId,
                PublicKey = settings[SettingsNames.Payment.PublicKey],
                PrivateKey = settings[SettingsNames.Payment.PrivateKey],
                Provider = settings[SettingsNames.Payment.Provider]
            };
        }

        /// <summary>
        /// <see cref="IPaymentService.ProcessSubscriptionPaymentAsync(Patient, string)"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="subscriptionId"></param>
        /// <returns></returns>
        public async Task<PaymentIntegrationModel?> ProcessSubscriptionPaymentAsync(Patient patient, string subscriptionId)
        {
            _logger.LogInformation($"Processing of payment for patient with [Id] = {patient.Id} started.");

            try
            {
                var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);

                await integrationService.BillSubscriptionAsync(subscriptionId);

                var invoices = await integrationService.GetPatientInvoicesAsync(
                    patient: patient,
                    updatedAt: _dateTimeProvider.UtcNow(),
                    createdAt: _dateTimeProvider.UtcNow()
                );

                var pendingInvoice = invoices.FirstOrDefault(x => x.SubscriptionId == subscriptionId && x.Status != "paid");

                if (pendingInvoice is null)
                {
                    _logger.LogInformation("Patient with [Id] does not have issued invoices.");
                    
                    // If pending invoice is null - it means subscription was paid automatically on the creation state
                    // And we don't have any payment issues in this case
                    return null;
                }

                return await integrationService.CreatePaymentAsync(patient, pendingInvoice.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Processing of payment for patient with [Id] = {patient.Id} failed. {ex}");
                throw;
            }
        }

        public async Task<SubscriptionIntegrationModel> BuySubscriptionAsyncV2(Patient patient,
            Subscription subscription,
            PaymentPrice paymentPrice,
            EmployerProduct? employerProduct,
            PromoCodeCoupon? promoCodeCoupon,
            bool isFirstPurchase,
            Subscription? previousSubscription = null)
        {
            // TODO: re-factor
            if (paymentPrice.Type is PaymentPriceType.PromoCode or PaymentPriceType.InsurancePromoCode)
            {
                throw new ValidationException($"Payment price can't of type '{paymentPrice.Type}' for the new PromoCode tool.");
            }
            
            if (employerProduct is not null)
            {
                // Overrides payment prices
                await ApplyDiscountAndChargePayorsAsync(
                    patient: patient,
                    subscription: subscription,
                    paymentPrice: paymentPrice,
                    employerProduct: employerProduct,
                    noStartupFee: false
                );
            }

            var subscriptionPrice = SubscriptionPriceDomain.Create(promoCodeCoupon, paymentPrice, employerProduct, _dateTimeProvider.UtcNow(), subscription.StartDate, isFirstPurchase);
            var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);
            
            return await integrationService.CreateOrUpdateSubscriptionAsync(
                patient: patient,
                paymentPrice: subscriptionPrice,
                previousSubscription: previousSubscription
            );
        }
        
        public async Task UpdateSubscriptionPriceAsync(int practiceId, string subscriptionId, SubscriptionPriceDomain subscriptionPrice)
        {
            var integrationService = await _integrationServiceFactory.CreateAsync(practiceId);
            await integrationService.UpdateSubscriptionPriceAsync(subscriptionId, subscriptionPrice);
        }

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
        public async Task<SubscriptionIntegrationModel> BuySubscriptionBackdatedAsync(
            Patient patient, 
            Subscription subscription,
            Subscription priorSubscription,
            PaymentPrice paymentPrice, 
            DateTime backdate, 
            bool noStartupFee)
        {
            if (subscription.EmployerProduct is not null)
            {
                await ApplyDiscountAndChargePayorsAsync(
                    patient: patient,
                    subscription: subscription,
                    priorSubscription: priorSubscription,
                    paymentPrice: paymentPrice,
                    employerProduct: subscription.EmployerProduct,
                    noStartupFee: noStartupFee
                );
            }
            
            var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);

            return await integrationService.CreateSubscriptionBackdatedAsync(
                patient: patient,
                paymentPrice: paymentPrice,
                backdate: subscription.StartDate,
                noStartupFee: noStartupFee,
                employerProduct: subscription.EmployerProduct
            );
        }

        /// <summary>
        /// Creates a subscription backdated and accounts for purchase payroll entries
        /// </summary>
        public async Task<SubscriptionIntegrationModel> BuySubscriptionBackdatedAsyncV2(
            Subscription subscription,
            Subscription priorSubscription,
            PromoCodeCoupon? coupon,
            Patient patient,
            PaymentPrice paymentPrice,
            EmployerProduct? employerProduct,
            bool chargeStartupFee)
        {
            // If the backdated request has a start date of today, we actually want to create the subscription and bill today, not on a backdate
            if (_dateTimeProvider.UtcNow().Date == subscription.StartDate.Date)
            {
                return await BuySubscriptionAsyncV2(
                    patient: patient,
                    subscription: subscription,
                    paymentPrice: paymentPrice,
                    employerProduct: employerProduct,
                    promoCodeCoupon: coupon,
                    false
                );
            }
            
            if (employerProduct is not null)
            {
                await ApplyDiscountAndChargePayorsAsync(
                    patient: patient,
                    subscription: subscription,
                    priorSubscription: priorSubscription,
                    paymentPrice: paymentPrice,
                    employerProduct: employerProduct,
                    noStartupFee: chargeStartupFee
                );
            }
            
            var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);
            
            var subscriptionPrice = SubscriptionPriceDomain.Create(coupon, paymentPrice, employerProduct, _dateTimeProvider.UtcNow(), subscription.StartDate, isFirstPurchase: chargeStartupFee);
            return await integrationService.CreateSubscriptionBackdatedAsyncV2(
                patient: patient,
                backdate: subscription.StartDate, 
                subscriptionPrice: subscriptionPrice);
        }

        /// <summary>
        /// <see cref="IPaymentService.ProcessOrdersPaymentAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="orders"></param>
        /// <param name="employerProduct"></param>
        /// <returns></returns>
        public async Task<PaymentIntegrationModel> ProcessOrdersPaymentAsync(Patient patient, Order[] orders, EmployerProduct? employerProduct)
        {
            _logger.LogInformation($"Processing of orders payment for patient with [Id] = {patient.Id} started.");
            
            var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);

            ///////////////////////////////////////////////////////////////
            // Want to record who paid for each order individually
            ///////////////////////////////////////////////////////////////
            foreach (var order in orders)
            {
                var addOns = order.Items
                    .Select(x => x.AddOn)
                    .ToArray();

                if (employerProduct is not null)
                {
                    await ApplyDiscountAndChargePayorsAsync(
                        patient: patient,
                        payable: order,
                        addOns: addOns,
                        employerProduct: employerProduct
                    );
                }
            }
            
            ///////////////////////////////////////////////////////////////
            // Want to pay for all of this collectively
            ///////////////////////////////////////////////////////////////
            var allAddOns = orders
                .SelectMany(o => o.Items)
                .Select(x => x.AddOn)
                .ToArray();

            // Need to go through each addOn, find the reference addOn to determine discounts and apply the discount
            foreach (var addOn in allAddOns)
            {
                var addOnParent = await _addOnsService.GetParentForAddOn(addOn, patient);

                _employerProductDiscountUtil.ApplyDiscount(
                    addOn: addOn,
                    employerProduct: employerProduct,
                    referenceAddOn: addOnParent);
            }

            var orderInvoiceId = orders.FirstOrDefault()?.OrderInvoiceIntegration?.Integration?.Value;
            
            var payment = await integrationService.BuyAddOnsProcessAsync(patient, allAddOns, orders, orderInvoiceId);
                
            _logger.LogInformation($"Processing of orders payment for patient with [Id] = {patient.Id} finished.");

            return payment;
        }

        /// <summary>
        /// <see cref="IPaymentService.ProcessFreeOrdersPaymentAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="orders"></param>
        /// <returns></returns>
        public async Task<PaymentIntegrationModel> ProcessFreeOrdersPaymentAsync(Patient patient, Order[] orders)
        {
            foreach (var order in orders)
            {
                var totalPrice = order.Items.Sum(x => x.Price);

                var defaultEmployer = await _defaultEmployerProvider.GetAsync();
                
                await _purchasePayorService.CreateAsync(
                    payable: order,
                    payor: defaultEmployer,
                    patient: patient,
                    amount: totalPrice, billableOnDate: _dateTimeProvider.UtcNow());
            }
            
            var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);

            return integrationService.ZeroPayment(patient);
        }
        
        /// <summary>
        /// <see cref="IPaymentService.CanPayForAddOnsAsync"/>
        /// </summary>
        /// <param name="addOns"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<bool> CanPayForAddOnsAsync(IEnumerable<AddOn> addOns, int practiceId)
        {
            var integrationService = await _integrationServiceFactory.CreateAsync(practiceId);

            return integrationService.CanPayForAddOns(addOns);
        }
        
        /// <summary>
        /// <see cref="IPaymentService.CreateCustomerPortalLinkAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        public async Task<string> CreateCustomerPortalLinkAsync(Patient patient)
        {
            var practiceId = patient.User.PracticeId;
            
            var integrationService = await _integrationServiceFactory.CreateAsync(practiceId);

            return await integrationService.CreateCustomerPortalSessionAsync(patient);
        }

        /// <summary>
        /// <see cref="IPaymentService.CreateResolveCustomerPortalLinkAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        public async Task<string> CreateResolveCustomerPortalLinkAsync(Patient patient)
        {
            var code = await _confirmCodeService.GenerateAsync(
                user: patient.User,
                type: ConfirmCodeType.CheckoutSession,
                option: ConfirmCodeOption.Guid,
                unlimited: true
            );

            return string.Format(_appOptions.ResolveCustomerPortalLinkUrl, _appOptions.ApiUrl, code.Code);
        }

        /// <summary>
        /// <see cref="IPaymentService.GenerateBuyProductPageAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public async Task<string> GenerateBuyProductPageAsync(Patient patient, Product product)
        {
            var practiceId = patient.User.PracticeId;
            
            var integrationService = await _integrationServiceFactory.CreateAsync(practiceId);

            var link = await integrationService.GenerateBuyProductLinkAsync(
                patient: patient, 
                productId: product.IntegrationId,
                price: product.GetPrice()
            );
            
            _backgroundJobsService.Schedule(
#pragma warning disable CS4014
                action: () => DisposeBuyProductPageAsync(practiceId, link.Id), // not awaited because it will be executed in the background job
#pragma warning restore CS4014
                delay: TimeSpan.FromMinutes(30),
                jobId: nameof(DisposeBuyProductPageAsync)
            );

            return link.Url;
        }

        /// <summary>
        /// <see cref="IPaymentService.DisposeBuyProductPageAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="linkId"></param>
        /// <returns></returns>
        public async Task DisposeBuyProductPageAsync(int practiceId, string linkId)
        {
            var integrationService = await _integrationServiceFactory.CreateAsync(practiceId);

            await integrationService.DisposeBuyProductLinkAsync(linkId);
        }

        /// <summary>
        /// <see cref="IPaymentService.GenerateInvoicePageAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="invoiceId"></param>
        /// <returns></returns>
        public async Task<string> GenerateInvoicePageAsync(Patient patient, string invoiceId)
        {
            var practiceId = patient.User.PracticeId;
            
            var integrationService = await _integrationServiceFactory.CreateAsync(practiceId);

            var link = await integrationService.GenerateInvoiceLinkAsync(
                patient: patient, 
                invoiceId: invoiceId
            );

            return link.Url;
        }

        /// <summary>
        /// <see cref="IPaymentService.RefundOrderPaymentAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<RefundIntegrationModel> RefundOrderPaymentAsync(int patientId, Order order)
        {
            var patient = await _patientsService.GetByIdAsync(patientId);

            _logger.LogInformation($"Refunding order payment for patient with [Id] = {patient.Id}, [OrderId] = {order.Id} started.");

            var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);

            var refund = await integrationService.CreateRefundAsync(patient, order.PaymentId);

            _logger.LogInformation($"Refunding order payment for patient with [Id] = {patient.Id}, [OrderId] = {order.Id} finished.");

            return refund;
        }

        #region private

        private async Task ApplyDiscountAndChargePayorsAsync(
            Patient patient,
            IPayable payable,
            AddOn[] addOns,
            EmployerProduct employerProduct)
        {
            var employerPrice = new decimal(0);
            var defaultEmployerPrice = new decimal(0);
            var defaultEmployer = await _defaultEmployerProvider.GetAsync();

            foreach (var addOn in addOns)
            {
                var addOnParent = await _addOnsService.GetParentForAddOn(addOn, patient);
                
                employerPrice += employerProduct.GetEmployerPrice(ProductType.AddOns, addOn.GetPrice(), addOnParent!.GetId());
                defaultEmployerPrice += employerProduct.GetDefaultEmployerPrice(ProductType.AddOns, addOn.GetPrice(), addOnParent.GetId());
                
                _employerProductDiscountUtil.ApplyDiscount(addOn, employerProduct, addOnParent);
            }
                
            await _purchasePayorService.CreateAsync(
                payable: payable,
                payor: employerProduct,
                patient: patient,
                amount: employerPrice, billableOnDate: _dateTimeProvider.UtcNow());
            
            await _purchasePayorService.CreateAsync(
                payable: payable,
                payor: defaultEmployer,
                patient: patient,
                amount: defaultEmployerPrice, billableOnDate: _dateTimeProvider.UtcNow());
            
            await _purchasePayorService.CreateAsync(
                payable: payable,
                payor: patient,
                patient: patient,
                amount: addOns.Sum(x => x.GetPrice()), billableOnDate: _dateTimeProvider.UtcNow());
        }

        private async Task ApplyDiscountAndChargePayorsAsync(
            Patient patient,
            Subscription subscription,
            PaymentPrice paymentPrice,
            EmployerProduct employerProduct,
            bool noStartupFee,
            Subscription? priorSubscription = null)
        {
            var defaultEmployer = await _defaultEmployerProvider.GetAsync();
            
            var employerSubscriptionPrice = employerProduct.GetEmployerPrice(
                productType: ProductType.Membership,
                originalPrice: paymentPrice.GetPrice()
            );
            
            var defaultEmployerSubscriptionPrice = employerProduct.GetDefaultEmployerPrice(
                productType: ProductType.Membership,
                originalPrice: paymentPrice.GetPrice()
            );

            // Want to also add the amount we are discounting due to coupon code to the amount defaultEmployer is paying for the subscription
            defaultEmployerSubscriptionPrice += paymentPrice.Discount;
            
            var employerStartupFeePrice = employerProduct.GetEmployerPrice(
                productType: ProductType.StartupFee,
                originalPrice: paymentPrice.GetStartupFee()
            );
            
            var defaultEmployerStartupFeePrice = employerProduct.GetDefaultEmployerPrice(
                productType: ProductType.StartupFee,
                originalPrice: paymentPrice.GetStartupFee()
            );

            _employerProductDiscountUtil.ApplyDiscount(
                paymentPrice: paymentPrice,
                employerProduct: employerProduct
            );

            var billingsAmount = paymentPrice.Strategy switch
            {
                PaymentStrategy.FullPayment => 1,
                PaymentStrategy.PartialPayment => paymentPrice.PaymentPeriod.PeriodInMonths,
                _ => throw new ArgumentException("Unsupported payment strategy")
            };

            var startDate = subscription.StartDate;
                
            for (var billingNumber = 0; billingNumber < billingsAmount; billingNumber++)
            {
                var billableOnDate = startDate.AddMonths(billingNumber);

                await _purchasePayorService.CreateAsync(
                    payable: subscription,
                    payor: employerProduct,
                    patient: patient,
                    amount: employerSubscriptionPrice,
                    billableOnDate: billableOnDate, isBilled: priorSubscription is not null && (priorSubscription.PurchasePayors.FirstOrDefault(o =>
                        o.PayorUniversalId == employerProduct.UniversalId && o.BillableOnDate == billableOnDate)?.IsBilled ?? false));
                
                await _purchasePayorService.CreateAsync(
                    payable: subscription,
                    payor: defaultEmployer,
                    patient: patient,
                    amount: defaultEmployerSubscriptionPrice,
                    billableOnDate: billableOnDate, isBilled: priorSubscription is not null && (priorSubscription.PurchasePayors.FirstOrDefault(o =>
                        o.PayorUniversalId == defaultEmployer?.UniversalId && o.BillableOnDate == billableOnDate)?.IsBilled ?? false));
                
                await _purchasePayorService.CreateAsync(
                    payable: subscription,
                    payor: patient,
                    patient: patient,
                    amount: paymentPrice.GetPrice(),
                    billableOnDate: billableOnDate, isBilled: priorSubscription is not null && (priorSubscription.PurchasePayors.FirstOrDefault(o =>
                        o.PayorUniversalId == patient.UniversalId && o.BillableOnDate == billableOnDate)?.IsBilled ?? false));
            }

            if (!noStartupFee)
            {
                await _purchasePayorService.CreateAsync(
                    payable: subscription,
                    payor: employerProduct,
                    patient: patient,
                    amount: employerStartupFeePrice, billableOnDate: startDate);
                
                await _purchasePayorService.CreateAsync(
                    payable: subscription,
                    payor: defaultEmployer,
                    patient: patient,
                    amount: defaultEmployerStartupFeePrice, billableOnDate: startDate);
                
                await _purchasePayorService.CreateAsync(
                    payable: subscription,
                    payor: patient,
                    patient: patient,
                    amount: paymentPrice.GetStartupFee(), billableOnDate: startDate);
            }
        }
        
        #endregion
    }
}
