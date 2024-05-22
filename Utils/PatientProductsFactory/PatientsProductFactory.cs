using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Services.Products;
using WildHealth.Application.Services.PurchasePayorService;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Application.Utils.DefaultEmployerProvider;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.Products;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Enums.Products;
using WildHealth.Domain.Interfaces;
using WildHealth.Domain.Models.Payment;
using WildHealth.Shared.Data.Repository;
using Subscription = WildHealth.Domain.Entities.Payments.Subscription;

namespace WildHealth.Application.Utils.PatientProductsFactory;

/// <summary>
/// <see cref="IPatientsProductFactory"/>
/// </summary>
public class PatientsProductFactory : IPatientsProductFactory
{
    private readonly IProductsService _productsService;
    private readonly IPurchasePayorService _purchasePayorService;
    private readonly IDefaultEmployerProvider _defaultEmployerProvider;
    private readonly IGeneralRepository<PatientProduct> _patientProductsRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PatientsProductFactory(
        IProductsService productsService,
        IPurchasePayorService purchasePayorService,
        IDefaultEmployerProvider defaultEmployerProvider,
        IGeneralRepository<PatientProduct> patientProductsRepository, 
        IDateTimeProvider dateTimeProvider)
    {
        _productsService = productsService;
        _purchasePayorService = purchasePayorService;
        _defaultEmployerProvider = defaultEmployerProvider;
        _patientProductsRepository = patientProductsRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>
    /// <see cref="IPatientsProductFactory.CreateBasedOnExistingAsync"/>
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="existing"></param>
    /// <param name="paymentFlow"></param>
    /// <returns></returns>
    public async Task<PatientProduct[]> CreateBasedOnExistingAsync(
        Patient patient,
        PatientProduct[] existing, 
        PaymentFlow paymentFlow)
    {
        var patientProducts = new List<PatientProduct>();

        foreach (var product in existing)
        {
            var patientProduct = new PatientProduct(
                patient: patient,
                productType: product.ProductType,
                productSubType: ProductSubType.Additional,
                paymentFlow: paymentFlow,
                price: product.Price,
                isLimited: product.IsLimited,
                sourceId: product.SourceId
            );

            // Do not put any record to purchase payor service until payment
            // If payment flow is Insurance. 
            if (patientProduct.PaymentFlow is not PaymentFlow.Insurance)
            {
                await _purchasePayorService.CreateAsync(
                    payable: patientProduct,
                    payor: patient,
                    patient: patient,
                    amount: product.Price,
                    billableOnDate: _dateTimeProvider.UtcNow()
                );
            }
            
            patientProducts.Add(patientProduct);
        }

        return patientProducts.ToArray();
    }

    /// <summary>
    /// <see cref="IPatientsProductFactory.CreateBuildInAsync(Patient, Subscription, PaymentCoupon)"/>
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="subscription"></param>
    /// <param name="coupon"></param>
    /// <returns></returns>
    public async Task<PatientProduct[]> CreateBuildInAsync(Patient patient, Subscription subscription, PaymentCoupon? coupon)
    {
        if (coupon is null || !coupon.OrderProducts || !coupon.OrderProductTypes.Any())
        {
            return Array.Empty<PatientProduct>();
        }

        var defaultEmployer = await _defaultEmployerProvider.GetAsync();
        
        var allProducts = await _productsService.GetAsync(patient.User.PracticeId);
        
        var patientProducts = new List<PatientProduct>();

        foreach (var productType in coupon.OrderProductTypes)
        {
            var correspondingProduct = allProducts.FirstOrDefault(x => x.Type == productType);
            
            if (correspondingProduct is null)
            {
                continue;
            }
            
            var patientProduct = new PatientProduct(
                patient: patient,
                productType: correspondingProduct.Type,
                productSubType: ProductSubType.BuiltIn,
                paymentFlow: GetPaymentFlow(correspondingProduct, subscription),
                price: correspondingProduct.GetPrice(),
                isLimited: correspondingProduct.IsLimited,
                sourceId: subscription.UniversalId
            );
            
            await _purchasePayorService.CreateAsync(
                payable: patientProduct,
                payor: defaultEmployer,
                patient: patient,
                amount: correspondingProduct.GetPrice(),
                billableOnDate: _dateTimeProvider.UtcNow()
            );
            
            patientProducts.Add(patientProduct);
        }

        return patientProducts.ToArray();
    }

    public async Task<PatientProduct[]> CreateBuildInAsyncV2(Patient patient, Subscription subscription, PromoCodeDomain coupon)
    {
        var freeAddOns = coupon.GetFreeAddOns();
        
        if(!freeAddOns.Any()) 
            return Array.Empty<PatientProduct>();
        
        var defaultEmployer = await _defaultEmployerProvider.GetAsync();
        
        var allProducts = await _productsService.GetAsync(patient.User.PracticeId);
        
        var patientProducts = new List<PatientProduct>();
        
        foreach (var productType in freeAddOns)
        {
            var correspondingProduct = allProducts.FirstOrDefault(x => x.Type == productType);
            
            if (correspondingProduct is null)
            {
                continue;
            }
            
            var patientProduct = new PatientProduct(
                patient: patient,
                productType: correspondingProduct.Type,
                productSubType: ProductSubType.BuiltIn,
                paymentFlow: GetPaymentFlow(correspondingProduct, subscription),
                price: correspondingProduct.GetPrice(),
                isLimited: correspondingProduct.IsLimited,
                sourceId: subscription.UniversalId
            );
            
            await _purchasePayorService.CreateAsync(
                payable: patientProduct,
                payor: defaultEmployer,
                patient: patient,
                amount: correspondingProduct.GetPrice(),
                billableOnDate: _dateTimeProvider.UtcNow()
            );
            
            patientProducts.Add(patientProduct);
        }

        return patientProducts.ToArray();
    }

    /// <summary>
    /// <see cref="IPatientsProductFactory.CreateAdditionalAsync"/>
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="subscription"></param>
    /// <param name="payor"></param>
    /// <param name="products"></param>
    /// <returns></returns>
    public async Task<PatientProduct[]> CreateAdditionalAsync(
        Patient patient, 
        Subscription subscription,
        IPayor payor,
        (ProductType type, int quantity)[] products)
    {
        if (products is null || !products.Any())
        {
            return Array.Empty<PatientProduct>();
        }
        
        var allProducts = await _productsService.GetAsync(patient.User.PracticeId);
        
        var patientProducts = new List<PatientProduct>();

        foreach (var (productType, quantity) in products)
        {
            var correspondingProduct = allProducts.FirstOrDefault(x => x.Type == productType);
            if (correspondingProduct is null)
            {
                continue;
            }
            
            for (var counter = 0; counter < quantity; counter++)
            {
                var patientProduct = new PatientProduct(
                    patient: patient,
                    productType: correspondingProduct.Type,
                    productSubType: ProductSubType.Additional,
                    paymentFlow: GetPaymentFlow(correspondingProduct, subscription),
                    price: correspondingProduct.GetPrice(),
                    isLimited: correspondingProduct.IsLimited
                );
            
                patientProducts.Add(patientProduct);
            }
        }

        return patientProducts.ToArray();
    }

    #region private

    /// <summary>
    /// Determines product payment flow depends on subscription type
    /// </summary>
    /// <param name="product"></param>
    /// <param name="subscription"></param>
    /// <returns></returns>
    private PaymentFlow GetPaymentFlow(Product product, Subscription subscription)
    {
        if (subscription is null)
        {
            return product.DefaultPaymentFlow ?? PaymentFlow.Regular;
        }

        var paymentFlow = ConvertToPaymentFlow(subscription.GetSubscriptionType());

        var supportedFlows = product.GetSupportedPaymentFlows();
        
        return supportedFlows.Contains(paymentFlow)
            ? paymentFlow
            : product.DefaultPaymentFlow ?? paymentFlow;
    }
    
    /// <summary>
    /// Determine number of existing products for this combination
    /// </summary>
    /// <param name="subscriptionUniversalId"></param>
    /// <param name="correspondingProductType"></param>
    /// <param name="productSubType"></param>
    /// <returns></returns>
    private async Task<int> GetNumberExistingPatientProducts(
        Guid subscriptionUniversalId, 
        ProductType correspondingProductType, 
        ProductSubType productSubType)
    {
        return await _patientProductsRepository
            .All()
            .CountAsync(o => o.SourceId == subscriptionUniversalId && o.ProductType == correspondingProductType && o.ProductSubType == productSubType);
    }
    
    /// <summary>
    /// Converts subscription type to product payment flow
    /// </summary>
    /// <param name="subscriptionType"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private PaymentFlow ConvertToPaymentFlow(SubscriptionType subscriptionType)
    {
        return subscriptionType switch
        {
            SubscriptionType.Regular => PaymentFlow.Regular,
            SubscriptionType.Insurance => PaymentFlow.Insurance,
            _ => throw new ArgumentOutOfRangeException(nameof(subscriptionType), subscriptionType, null)
        };
    }
        
    #endregion
}