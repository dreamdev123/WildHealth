using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.Products;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Enums.Products;

namespace WildHealth.Application.CommandHandlers.Payments.Flows;

public class CreateBuiltInProductsFlow : IMaterialisableFlow
{
    private readonly Subscription _subscription;
    private readonly PatientProduct[] _subscriptionProducts;
    private readonly Product[] _allProducts;

    public CreateBuiltInProductsFlow(Subscription subscription, 
        PatientProduct[] subscriptionProducts,
        Product[] allProducts)
    {
        _subscription = subscription;
        _subscriptionProducts = subscriptionProducts;
        _allProducts = allProducts;
    }

    public MaterialisableFlowResult Execute()
    {
        var inclusions = _subscription.GetInclusions().ToList();
        if (!inclusions.Any())
        {
            return MaterialisableFlowResult.Empty;
        }

        var paymentFlow = ConvertToPaymentFlow();
        var patientProducts = inclusions.SelectMany(inclusion =>
        {
            var correspondingProduct = _allProducts.FirstOrDefault(x => x.Type == inclusion.ProductType);

            if (correspondingProduct is null)
                return Array.Empty<PatientProduct>();

            // If default payment flow doesn't match patient payment flow
            // Do not create build in product
            if (correspondingProduct.Price > 0 && correspondingProduct.DefaultPaymentFlow != paymentFlow)
                return Array.Empty<PatientProduct>();

            var numberExistingPatientProducts = GetNumberExistingPatientProducts(productType: correspondingProduct.Type, productSubType: ProductSubType.BuiltIn);
            var numberPatientProductsToCreate = inclusion.Count - numberExistingPatientProducts;
            
            var newProducts = new List<PatientProduct>();
            for (var n = 0; n < numberPatientProductsToCreate; n++)
            {
                var patientProduct = new PatientProduct(
                    patient: _subscription.Patient,
                    productType: correspondingProduct.Type,
                    ProductSubType.BuiltIn,
                    paymentFlow: paymentFlow,
                    expiredAt: _subscription.EndDate,
                    sourceId: _subscription.UniversalId,
                    isLimited: inclusion.IsLimited ?? correspondingProduct.IsLimited,
                    price: 0
                );
                
                // Intentionally not adding purchasePayor entry here.
                // Since the product is built-in it's accounted for as part of the subscription purchase
                newProducts.Add(patientProduct);
            }
            
            return newProducts.ToArray();
        });

        return patientProducts.Select(p => p.Added()).ToFlowResult();
    }

    private int GetNumberExistingPatientProducts(ProductType productType, ProductSubType productSubType)
    {
        return _subscriptionProducts.Count(o => 
                o.SourceId == _subscription.UniversalId && 
                o.ProductType == productType && 
                o.ProductSubType == productSubType);
    }

    private PaymentFlow ConvertToPaymentFlow()
    {
        var subscriptionType = _subscription.GetSubscriptionType();
        return subscriptionType switch
        {
            SubscriptionType.Regular => PaymentFlow.Regular,
            SubscriptionType.Insurance => PaymentFlow.Insurance,
            _ => throw new ArgumentOutOfRangeException(nameof(subscriptionType), subscriptionType, null)
        };
    }
}