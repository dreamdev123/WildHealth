using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using WildHealth.Integration.Events;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Products;
using WildHealth.Domain.Entities.Products;
using WildHealth.Integration.Models.Orders;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Utils.PatientProductsFactory;
using MediatR;
using WildHealth.Application.Services.PurchasePayorService;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Domain.Enums.Products;

namespace WildHealth.Application.EventHandlers.Integration;

public class IntegrationProductPurchasedEventHandler : INotificationHandler<IntegrationProductPurchasedEvent>
{
    private readonly IPatientProductsService _patientProductsService;
    private readonly IPatientsProductFactory _patientsProductFactory;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IProductsService _productsService;
    private readonly IPatientsService _patientsService;
    private readonly IPurchasePayorService _purchasePayorService;
    private readonly ILogger _logger;

    public IntegrationProductPurchasedEventHandler(
        IPatientProductsService patientProductsService, 
        IPatientsProductFactory patientsProductFactory,
        ISubscriptionService subscriptionService,
        IProductsService productsService, 
        IPatientsService patientsService, 
        IPurchasePayorService purchasePayorService,
        ILogger<IntegrationProductPurchasedEventHandler> logger)
    {
        _patientProductsService = patientProductsService;
        _patientsProductFactory = patientsProductFactory;
        _subscriptionService = subscriptionService;
        _productsService = productsService;
        _patientsService = patientsService;
        _purchasePayorService = purchasePayorService;
        _logger = logger;
    }

    public async Task Handle(IntegrationProductPurchasedEvent @event, CancellationToken cancellationToken)
    {
        var order = @event.Order;

        if (!AssertOrderIsCompleted(order))
        {
            _logger.LogInformation($"Skipped integration order with [IntegrationOrderId]: {order.Id} because it's not completed");
            
            return;
        }

        var patient = await GetPatientAsync(order.CustomerId, @event.Vendor);
        if (patient is null)
        {
            _logger.LogInformation($"Patient with [IntegrationId]: {order.CustomerId} does not exist");
            
            return;
        }

        var currentSubscription = await _subscriptionService.GetCurrentSubscriptionAsync(patient.GetId());

        var allProducts = await _productsService.GetAsync(patient.User.PracticeId);

        var purchasedProducts = ExtractProductsFromOrder(order, allProducts);

        if (!purchasedProducts.Any())
        {
            _logger.LogInformation($"Integration order with [IntegrationOrderId]: {order.Id} doesn't contain products");

            return;
        }

        var patientProducts = await _patientsProductFactory.CreateAdditionalAsync(
            patient: patient,
            payor: patient,
            products: purchasedProducts,
            subscription: currentSubscription
        );

        foreach (var patientProduct in patientProducts)
        {
            await _purchasePayorService.CreateAsync(
                payable: patientProduct,
                payor: patient,
                patient: patient,
                amount: patientProduct.Price,
                billableOnDate: DateTime.UtcNow
            );
            
            patientProduct.MarkAsPaid(integrationId: order.Id);
        }

        await _patientProductsService.CreateAsync(patientProducts);
    }

    #region private

    /// <summary>
    /// Asserts order is completed
    /// </summary>
    /// <param name="order"></param>
    /// <returns></returns>
    private bool AssertOrderIsCompleted(OrderIntegrationModel order)
    {
        if (order is null)
        {
            return false;
        }

        return order.IsCompleted;
    }
    
    /// <summary>
    /// Extracts and returns products from the order
    /// </summary>
    /// <param name="order"></param>
    /// <param name="allProducts"></param>
    /// <returns></returns>
    private (ProductType type, int quantity)[] ExtractProductsFromOrder(OrderIntegrationModel order, Product[] allProducts)
    {
        var items = order.Items;
        var products = new List<Product>();

        foreach (var item in items)
        {
            for (int quantity = 0; quantity < item.Quantity; quantity++)
            {
                var product = allProducts.FirstOrDefault(x => x.IntegrationId == item.ProductId);

                if (product is not null)
                {
                    products.Add(product);
                }
            }
        }

        return products
            .GroupBy(x => x.Type)
            .Select(group => (group.Key, group.Count()))
            .ToArray();
    }

    /// <summary>
    /// Fetches and returns patient by integration id
    /// </summary>
    /// <param name="integrationId"></param>
    /// <param name="vendor"></param>
    /// <returns></returns>
    private async Task<Patient?> GetPatientAsync(string integrationId, IntegrationVendor vendor)
    {
        try
        {
            return await _patientsService.GetByIntegrationIdAsync(integrationId, vendor);
        }
        catch (AppException e) when(e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    #endregion
}