using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Products;
using WildHealth.Application.Services.EmployerProducts;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Products;
using WildHealth.Application.Services.ProductsPayment;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Utils.PatientProductsFactory;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Products;
using WildHealth.Domain.Enums.Products;
using WildHealth.Integration.Models.Payments;
using MediatR;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Products;

public class BuyProductsCommandHandler : IRequestHandler<BuyProductsCommand, PatientProduct[]>
{
    private readonly IPatientProductsService _patientProductsService;
    private readonly IPatientsProductFactory _patientsProductFactory;
    private readonly IEmployerProductService _employerProductService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IProductsPaymentService _paymentService;
    private readonly IPatientsService _patientsService;
    private readonly IProductsService _productsService;
    private readonly ILogger _logger;

    public BuyProductsCommandHandler(
        IPatientProductsService patientProductsService, 
        IPatientsProductFactory patientsProductFactory,
        IEmployerProductService employerProductService,
        ISubscriptionService subscriptionService,
        IProductsPaymentService paymentService,
        IPatientsService patientsService, 
        IProductsService productsService,
        ILogger<BuyProductsCommandHandler> logger)
    {
        _patientProductsService = patientProductsService;
        _patientsProductFactory = patientsProductFactory;
        _employerProductService = employerProductService;
        _subscriptionService = subscriptionService;
        _patientsService = patientsService;
        _productsService = productsService;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<PatientProduct[]> Handle(BuyProductsCommand command, CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetByIdAsync(command.PatientId);

        var currentSubscription = await _subscriptionService.GetCurrentSubscriptionAsync(patient.GetId());
        
        var employerProduct = await GetEmployerProductAsync(patient);
        
        var allProducts = await _productsService.GetAsync(patient.User.PracticeId);

        var normalizedProducts = NormalizeRequestedProducts(command.Products, allProducts);

        AssertCanBuyProducts(normalizedProducts.Select(x => x.product).ToArray());
            
        var patientProducts = await _patientsProductFactory.CreateAdditionalAsync(
            patient: patient, 
            payor: patient,
            subscription: currentSubscription,
            products: command.Products
        );

        await _patientProductsService.CreateAsync(patientProducts);
        
        var groups = patientProducts.GroupBy(x => x.PaymentFlow);

        foreach (var group in groups)
        {
            var groupedProducts = group.ToArray();
            
            PaymentIntegrationModel payment;
        
            try
            {
                payment = await _paymentService.ProcessProductsPaymentAsync(
                    patient: patient, 
                    patientProducts: groupedProducts,
                    products: normalizedProducts,
                    employerProduct: employerProduct,
                    isPaidByDefaultEmployer: command.IsPaidByDefaultEmployer
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Buying products for patient [PatientId] = {patient.Id} failed.", ex);
                await _patientProductsService.DeleteAsync(patientProducts);
                throw;
            }

            // For products with insurance flow payment will be NULL according to logic.
            // Particular product will be paid with a copay + charged insurance
            if (payment is not null)
            {
                foreach (var product in groupedProducts)
                {
                    product.MarkAsPaid(payment.Id);
                }
            }

            await _patientProductsService.UpdateAsync(groupedProducts);
        }

        return patientProducts;
    }
    
    #region private

    /// <summary>
    /// Assert can purchase corresponding products
    /// </summary>
    /// <param name="product"></param>
    private void AssertCanBuyProducts(Product[] product)
    {
        if (product.Any(x => !x.CanBuyProduct()))
        {
            throw new AppException(HttpStatusCode.BadRequest, "Can not buy unlimited product");
        }
    }
    
    /// <summary>
    /// Normalizes requested products
    /// </summary>
    /// <param name="requestedProducts"></param>
    /// <param name="allProducts"></param>
    /// <returns></returns>
    private (Product product, int quantity)[] NormalizeRequestedProducts((ProductType type, int quantity)[] requestedProducts, Product[] allProducts)
    {
        var normalizedProducts = new List<(Product product, int quantity)>();

        foreach (var (type, quantity) in requestedProducts)
        {
            var correspondingProduct = allProducts.First(x => x.Type == type);
            
            normalizedProducts.Add((correspondingProduct, quantity));
        }

        return normalizedProducts.ToArray();
    }

    
    /// <summary>
    /// Fetches and returns employer product related to current subscription
    /// </summary>
    /// <param name="patient"></param>
    /// <returns></returns>
    private Task<EmployerProduct> GetEmployerProductAsync(Patient patient)
    {
        var currentSubscription = patient.CurrentSubscription;

        if (currentSubscription is null || currentSubscription.ProductId is null)
        {
            return _employerProductService.GetByKeyAsync(string.Empty);
        }

        return _employerProductService.GetByIdAsync(currentSubscription.ProductId.Value);
    }

    #endregion
}