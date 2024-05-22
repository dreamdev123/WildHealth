using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Products;
using WildHealth.Application.Services.ProductsPayment.Insurance;
using WildHealth.Application.Services.ProductsPayment.Regular;
using WildHealth.Application.Services.PurchasePayorService;
using WildHealth.Application.Utils.ApplyEmployerUtil;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Application.Utils.DefaultEmployerProvider;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Products;
using WildHealth.Domain.Enums.Products;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Models.Invoices;
using WildHealth.Integration.Models.Payments;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.ProductsPayment;

/// <summary>
/// <see cref="IProductsPaymentService"/>
/// </summary>
public class ProductsPaymentService : IProductsPaymentService
{
    private readonly IDictionary<PaymentFlow, IProductsPaymentService> _paymentServices;

    public ProductsPaymentService(
        IIntegrationServiceFactory integrationServiceFactory,
        IPurchasePayorService purchasePayorService,
        IEmployerProductDiscountUtil employerProductDiscountUtil,
        IDefaultEmployerProvider defaultEmployerProvider,
        IDateTimeProvider dateTimeProvider,
        IProductsService productsService,
        ILoggerFactory loggerFactory)
    {
        var regularPaymentService = new RegularProductsPaymentService(
            integrationServiceFactory: integrationServiceFactory,
            purchasePayorService: purchasePayorService,
            employerProductDiscountUtil: employerProductDiscountUtil,
            defaultEmployerProvider: defaultEmployerProvider,
            dateTimeProvider: dateTimeProvider,
            productsService: productsService,
            logger: loggerFactory.CreateLogger<RegularProductsPaymentService>()
        );

        var insurancePaymentService = new InsuranceProductsPaymentService(
            integrationServiceFactory: integrationServiceFactory,
            purchasePayorService: purchasePayorService,
            defaultEmployerProvider: defaultEmployerProvider,
            dateTimeProvider: dateTimeProvider,
            logger: loggerFactory.CreateLogger<InsuranceProductsPaymentService>()
        );

        _paymentServices = new Dictionary<PaymentFlow, IProductsPaymentService>
        {
            { PaymentFlow.Regular, regularPaymentService },
            { PaymentFlow.Insurance, insurancePaymentService }
        };
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
        var flow = patientProducts.First().PaymentFlow;

        if (patientProducts.All(x => x.PaymentFlow != flow))
        {
            throw new AppException(HttpStatusCode.BadRequest, "All products must have the same payment flow.");
        }

        var service = _paymentServices[flow];

        return service.ProcessProductsPaymentAsync(
            patient: patient,
            products: products,
            patientProducts: patientProducts,
            employerProduct: employerProduct,
            isPaidByDefaultEmployer: isPaidByDefaultEmployer
        );
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
        string? memo)
    {
        var service = _paymentServices[PaymentFlow.Insurance];

        return service.ProcessProductPatientResponsibilityPaymentAsync(
            patient: patient,
            items: items,
            accountTaxId: accountTaxId,
            memo: memo
        );
    }
}