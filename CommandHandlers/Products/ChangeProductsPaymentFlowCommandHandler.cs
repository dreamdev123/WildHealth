using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Products;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Utils.PatientProductsFactory;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Products;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.ProductsPayment;
using WildHealth.Integration.Models.Payments;
using WildHealth.Application.Services.Products;
using WildHealth.Domain.Entities.Products;
using WildHealth.Application.Services.EmployerProducts;
using WildHealth.Domain.Entities.EmployerProducts;
using MediatR;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.CommandHandlers.Products;

public class ChangeProductsPaymentFlowCommandHandler : IRequestHandler<SubstituteProductsCommand>
{
    private static readonly IDictionary<PaymentFlow, ProductPaymentStatus[]> InterestingStatuses =
        new Dictionary<PaymentFlow, ProductPaymentStatus[]>
        {
            {
                PaymentFlow.Regular,
                new []
                {
                    ProductPaymentStatus.Paid,
                    ProductPaymentStatus.NotPayable
                }
            },
            {
                PaymentFlow.Insurance,
                new []
                {
#pragma warning disable CS0618
                    ProductPaymentStatus.PendingCopay, // NOTE: this status is obsolete but needed for backward compatibility
#pragma warning restore CS0618
                    ProductPaymentStatus.PendingInsuranceClaim,
                    ProductPaymentStatus.PendingOutstandingInvoice
                }
            }
        };

    private readonly IPatientProductsService _patientProductsService;
    private readonly IPatientsProductFactory _patientsProductFactory;
    private readonly IProductsPaymentService _productsPaymentService;
    private readonly IEmployerProductService _employerProductService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IProductsService _productsService;
    private readonly IPatientsService _patientsService;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public ChangeProductsPaymentFlowCommandHandler(
        IPatientProductsService patientProductsService, 
        IPatientsProductFactory patientsProductFactory, 
        IProductsPaymentService productsPaymentService, 
        IEmployerProductService employerProductService,
        IDateTimeProvider dateTimeProvider,
        IProductsService productsService, 
        IPatientsService patientsService, 
        IMediator mediator, 
        ILogger<ChangeProductsPaymentFlowCommandHandler> logger)
    {
        _patientProductsService = patientProductsService;
        _patientsProductFactory = patientsProductFactory;
        _productsPaymentService = productsPaymentService;
        _employerProductService = employerProductService;
        _dateTimeProvider = dateTimeProvider;
        _productsService = productsService;
        _patientsService = patientsService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(SubstituteProductsCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Started changing payment flow for products related to patient with [Id] = {command.PatientId}");

        var specification = PatientSpecifications.PatientWithIntegrationsAndUser;
        
        var patient = await _patientsService.GetByIdAsync(command.PatientId, specification);
        
        var employerProduct = await GetEmployerProductAsync(patient);

        var allProducts = await _productsService.GetAsync(patient.User.PracticeId);
        
        var productsToReplace = await GetPatientProductsAsync(
            patientId: command.PatientId,
            paymentFlow: command.OldPaymentFlow
        );

        var availableProducts = await GetAvailablePatientProductsAsync(
            patientId: patient.GetId(),
            except: productsToReplace
        );

        var productsToCreate = GetProductsNeedToBeCreated(
            productsToReplace: productsToReplace,
            availableProducts: availableProducts
        );

        var newProducts = await _patientsProductFactory.CreateBasedOnExistingAsync(
            patient: patient,
            existing: productsToCreate,
            paymentFlow: command.NewPaymentFlow
        );
            
        await _patientProductsService.CreateAsync(newProducts);

        await PayProductsAsync(
            patient: patient,
            newProducts: newProducts,
            allProducts: allProducts,
            employerProduct: employerProduct
        );

        await SubstituteProductsAsync(
            oldProducts: productsToReplace,
            newProducts: newProducts, 
            availableProducts: availableProducts
        );
        
        await VoidOldProductsAsync(productsToReplace);

        _logger.LogInformation($"Finished changing payment flow for products related to patient with [Id] = {command.PatientId}");
    }
    
    #region private

    /// <summary>
    /// Fetches and returns patient products
    /// </summary>
    /// <param name="patientId"></param>
    /// <param name="paymentFlow"></param>
    /// <returns></returns>
    private Task<PatientProduct[]> GetPatientProductsAsync(int patientId, PaymentFlow paymentFlow)
    {
        var now = _dateTimeProvider.UtcNow();
        
        return _patientProductsService.SelectAsync(
            paymentStatuses: InterestingStatuses[paymentFlow],
            paymentFlow: paymentFlow,
            usedFrom: now,
            usedTo: null,
            patientId: patientId,
            productTypes: null
        );
    }

    /// <summary>
    /// Returns available patient products
    /// </summary>
    /// <param name="patientId"></param>
    /// <param name="except"></param>
    /// <returns></returns>
    private async Task<PatientProduct[]> GetAvailablePatientProductsAsync(int patientId, PatientProduct[] except)
    {
        var availableProducts = await _patientProductsService.GetActiveAsync(patientId);

        return availableProducts.Where(x => except.All(t => x.Id != t.Id)).ToArray();
    }

    /// <summary>
    /// Returns products which can't be replaced by available and needs to be created
    /// </summary>
    /// <param name="productsToReplace"></param>
    /// <param name="availableProducts"></param>
    /// <returns></returns>
    private PatientProduct[] GetProductsNeedToBeCreated(
        PatientProduct[] productsToReplace,
        PatientProduct[] availableProducts)
    {
        var productsNeedToBeCreated = new List<PatientProduct>();
        var usedProducts = new List<PatientProduct>();

        foreach (var product in productsToReplace)
        {
            var altProduct = availableProducts.FirstOrDefault(x => x.ProductType == product.ProductType && usedProducts.All(t => t.Id != x.Id));

            if (altProduct is not null)
            {
                usedProducts.Add(altProduct);
                
                continue;
            }
            
            productsNeedToBeCreated.Add(product);
        }

        return productsNeedToBeCreated.ToArray();
    }
    
    /// <summary>
    /// Normalizes requested products
    /// </summary>
    /// <param name="patientProducts"></param>
    /// <param name="allProducts"></param>
    /// <returns></returns>
    private (Product product, int quantity)[] NormalizeRequestedProducts(PatientProduct[] patientProducts, Product[] allProducts)
    {
        var normalizedProducts = new List<(Product product, int quantity)>();

        foreach (var patientProduct in patientProducts)
        {
            var correspondingProduct = allProducts.First(x => x.Type == patientProduct.ProductType);
            
            normalizedProducts.Add((correspondingProduct, 1));
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
    
    private async Task PayProductsAsync(
        Patient patient, 
        PatientProduct[] newProducts,
        Product[] allProducts,
        EmployerProduct employerProduct)
    {
        var groups = newProducts.GroupBy(x => x.PaymentFlow);

        var normalizedProducts = NormalizeRequestedProducts(
            patientProducts: newProducts,
            allProducts: allProducts
        );
        
        foreach (var group in groups)
        {
            var groupedProducts = group.ToArray();
            
            PaymentIntegrationModel payment;
        
            try
            {
                payment = await _productsPaymentService.ProcessProductsPaymentAsync(
                    patient: patient, 
                    patientProducts: groupedProducts,
                    products: normalizedProducts,
                    employerProduct: employerProduct,
                    isPaidByDefaultEmployer: false
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Buying products for patient [PatientId] = {patient.Id} failed.", ex);
                await _patientProductsService.DeleteAsync(newProducts);
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
    }

    /// <summary>
    /// Void existing products
    /// </summary>
    /// <param name="patientProducts"></param>
    private async Task VoidOldProductsAsync(PatientProduct[] patientProducts)
    {
        foreach (var patientProduct in patientProducts)
        {
            var command = new VoidProductCommand(patientProduct.GetId());

            await _mediator.Send(command);
        }
    }

    /// <summary>
    /// Substitute old products by new products for all services
    /// </summary>
    /// <param name="oldProducts"></param>
    /// <param name="newProducts"></param>
    /// <param name="availableProducts"></param>
    private async Task SubstituteProductsAsync(PatientProduct[] oldProducts, PatientProduct[] newProducts, PatientProduct[] availableProducts)
    {
        foreach (var oldProduct in oldProducts)
        {
            var newProduct = newProducts.FirstOrDefault(x => x.ProductType == oldProduct.ProductType && !x.IsUsed);

            if (newProduct is null)
            {
                newProduct = availableProducts.First(x => x.ProductType == oldProduct.ProductType && !x.IsUsed);
            }
            
            newProduct.Substitute(oldProduct);
        }

        await _patientProductsService.UpdateAsync(newProducts);
    }
    
    #endregion
}