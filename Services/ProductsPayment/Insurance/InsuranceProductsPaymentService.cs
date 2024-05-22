using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.PurchasePayorService;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Application.Utils.DefaultEmployerProvider;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Products;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Models.Invoices;
using WildHealth.Integration.Models.Payments;

namespace WildHealth.Application.Services.ProductsPayment.Insurance;

/// <summary>
/// <see cref="IProductsPaymentService"/>
/// </summary>
public class InsuranceProductsPaymentService : IProductsPaymentService
{
    private readonly IIntegrationServiceFactory _integrationServiceFactory;
    private readonly IPurchasePayorService _purchasePayorService;
    private readonly IDefaultEmployerProvider _defaultEmployerProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger _logger;

    public InsuranceProductsPaymentService(
        IIntegrationServiceFactory integrationServiceFactory,
        IPurchasePayorService purchasePayorService,
        IDefaultEmployerProvider defaultEmployerProvider,
        IDateTimeProvider dateTimeProvider,
        ILogger<InsuranceProductsPaymentService> logger)
    {
        _integrationServiceFactory = integrationServiceFactory;
        _purchasePayorService = purchasePayorService;
        _defaultEmployerProvider = defaultEmployerProvider;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }
    
    /// <summary>
    /// <see cref="IProductsPaymentService.ProcessProductsPaymentAsync"/>
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="products"></param>
    /// <param name="patientProducts"></param>
    /// <param name="employerProduct"></param>
    /// <param name="isPaidByDefaultEmployer"></param>
    /// <returns></returns>
    public Task<PaymentIntegrationModel> ProcessProductsPaymentAsync(
        Patient patient,
        (Product product, int quantity)[] products,
        PatientProduct[] patientProducts,
        EmployerProduct employerProduct,
        bool isPaidByDefaultEmployer)
    {
        return Task.FromResult<PaymentIntegrationModel>(null!);
    }

    /// <summary>
    /// <see cref="IProductsPaymentService.ProcessProductPatientResponsibilityPaymentAsync"/>
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="items"></param>
    /// <param name="accountTaxId"></param>
    /// <param name="memo"></param>
    /// <returns></returns>
    public async Task<InvoiceIntegrationModel> ProcessProductPatientResponsibilityPaymentAsync(
        Patient patient, 
        (string productId, decimal price)[] items,
        string accountTaxId,
        string? memo = null)
    {
        _logger.LogInformation($"Processing of products payment for patient with [Id] = {patient.Id} started.");

        var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);

        var payment = await integrationService.ChargeProductPatientResponsibilityAsync(
            patient: patient,
            items: items,
            accountTaxId: accountTaxId,
            memo: memo
        );

        _logger.LogInformation($"Processing of orders payment for patient with [Id] = {patient.Id} finished.");

        return payment;
    }

    #region private

    /// <summary>
    /// Apply copay discount and charges payor
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="product"></param>
    /// <param name="patientProduct"></param>
    /// <param name="employerProduct"></param>
    /// <param name="verification"></param>
    private async Task ApplyDiscountAndChargePayorsAsync(
        Patient patient,
        Product product,
        PatientProduct patientProduct,
        EmployerProduct employerProduct,
        InsuranceVerification verification)
    {
        var defaultEmployer = await _defaultEmployerProvider.GetAsync();

        var employerPrice = employerProduct.GetEmployerPrice(
            productType: patientProduct.ProductType,
            originalPrice: verification.Copay ?? Decimal.Zero
        );

        var defaultEmployerPrice = employerProduct.GetDefaultEmployerPrice(
            productType: patientProduct.ProductType,
            originalPrice: verification.Copay ?? Decimal.Zero
        );

        await _purchasePayorService.CreateAsync(
            payable: patientProduct,
            payor: employerProduct,
            patient: patient,
            amount: employerPrice, 
            billableOnDate: _dateTimeProvider.UtcNow());

        await _purchasePayorService.CreateAsync(
            payable: patientProduct,
            payor: defaultEmployer,
            patient: patient,
            amount: defaultEmployerPrice, 
            billableOnDate: _dateTimeProvider.UtcNow());

        await _purchasePayorService.CreateAsync(
            payable: patientProduct,
            payor: patient,
            patient: patient,
            amount: verification.Copay ?? Decimal.Zero,
            billableOnDate: _dateTimeProvider.UtcNow());
    }

    #endregion
}