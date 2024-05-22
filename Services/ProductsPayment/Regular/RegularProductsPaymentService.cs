using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Products;
using WildHealth.Application.Services.PurchasePayorService;
using WildHealth.Application.Utils.ApplyEmployerUtil;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Application.Utils.DefaultEmployerProvider;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Products;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Models.Invoices;
using WildHealth.Integration.Models.Payments;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.ProductsPayment.Regular;

/// <summary>
/// <see cref="IProductsPaymentService"/>
/// </summary>
public class RegularProductsPaymentService : IProductsPaymentService
{
    private readonly IIntegrationServiceFactory _integrationServiceFactory;
    private readonly IPurchasePayorService _purchasePayorService;
    private readonly IEmployerProductDiscountUtil _employerProductDiscountUtil;
    private readonly IDefaultEmployerProvider _defaultEmployerProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IProductsService _productsService;
    private readonly ILogger<RegularProductsPaymentService> _logger;

    public RegularProductsPaymentService(
        IIntegrationServiceFactory integrationServiceFactory,
        IPurchasePayorService purchasePayorService,
        IEmployerProductDiscountUtil employerProductDiscountUtil,
        IDefaultEmployerProvider defaultEmployerProvider,
        IDateTimeProvider dateTimeProvider,
        IProductsService productsService,
        ILogger<RegularProductsPaymentService> logger)
    {
        _integrationServiceFactory = integrationServiceFactory;
        _purchasePayorService = purchasePayorService;
        _employerProductDiscountUtil = employerProductDiscountUtil;
        _defaultEmployerProvider = defaultEmployerProvider;
        _dateTimeProvider = dateTimeProvider;
        _productsService = productsService;
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
    public async Task<PaymentIntegrationModel> ProcessProductsPaymentAsync(
        Patient patient,
        (Product product, int quantity)[] products,
        PatientProduct[] patientProducts,
        EmployerProduct employerProduct,
        bool isPaidByDefaultEmployer)
    {
        _logger.LogInformation($"Processing of products payment for patient with [Id] = {patient.Id} started.");

        var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);

        if (employerProduct is not null)
        {
            await ApplyDiscountAndChargePayorsAsync(
                patient: patient,
                patientProducts: patientProducts,
                employerProduct: employerProduct,
                isPaidByDefaultEmployer: isPaidByDefaultEmployer
            );
        }

        var payment = await integrationService.BuyProductsProcessAsync(patient, products);

        if (payment is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Products purchasing failed, please, try again later.");
        }
        
        _logger.LogInformation($"Processing of orders payment for patient with [Id] = {patient.Id} finished.");

        return payment;
    }

    /// <summary>
    /// <see cref="IProductsPaymentService.ProcessProductPatientResponsibilityPaymentAsync"/>
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="items"></param>
    /// <param name="accountTaxId"></param>
    /// <param name="memo"></param>
    /// <returns></returns>
    public Task<InvoiceIntegrationModel> ProcessProductPatientResponsibilityPaymentAsync(
        Patient patient, 
        (string productId, decimal price)[] items,
        string accountTaxId,
        string? memo = null)
    {
        throw new AppException(HttpStatusCode.BadRequest, "This product type doesn't support this operation");
    }

    #region private

    private async Task ApplyDiscountAndChargePayorsAsync(
        Patient patient,
        PatientProduct[] patientProducts,
        EmployerProduct employerProduct,
        bool isPaidByDefaultEmployer)
    {
        var defaultEmployer = await _defaultEmployerProvider.GetAsync();

        var allProducts = await _productsService.GetAsync(patient.User.PracticeId);

        foreach (var patientProduct in patientProducts)
        {
            var product = allProducts.FirstOrDefault(o => o.Type == patientProduct.ProductType);

            if (product == null)
            {
                _logger.LogError(
                    $"Critical error - unable to locate product for [PatientProductId] = {patientProduct.GetId()}");

                continue;
            }

            // If being paid for by default employer, then give full charge to them and go to next
            if (isPaidByDefaultEmployer)
            {
                _employerProductDiscountUtil.ApplyDefaultEmployerDiscount(product, defaultEmployer);
                
                await _purchasePayorService.CreateAsync(
                    payable: patientProduct,
                    payor: defaultEmployer,
                    patient: patient,
                    amount: patientProduct.Price, 
                    billableOnDate: _dateTimeProvider.UtcNow());
                
                continue;
            }

            var employerPrice = employerProduct.GetEmployerPrice(
                productType: patientProduct.ProductType,
                originalPrice: patientProduct.Price
            );

            var defaultEmployerPrice = employerProduct.GetDefaultEmployerPrice(
                productType: patientProduct.ProductType,
                originalPrice: patientProduct.Price
            );

            _employerProductDiscountUtil.ApplyDiscount(product, employerProduct);

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
                amount: product.GetPrice(), 
                billableOnDate: _dateTimeProvider.UtcNow());
        }
    }

    #endregion
}