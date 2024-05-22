using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Commands.Payments;
using WildHealth.Common.Models.Payments;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Models.Prices;
using WildHealth.Integration.Services.HintStripe;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;
using MediatR;
using Microsoft.Extensions.Options;
using WildHealth.Common.Options;

namespace WildHealth.Application.CommandHandlers.Payments;

public class MigratePaymentIntegrationCommandHandler : IRequestHandler<MigratePaymentIntegrationCommand, ICollection<PaymentPriceMigrationReport>>
{
    private readonly IGeneralRepository<PaymentPrice> _paymentPriceRepository;
    private readonly IIntegrationServiceFactory _integrationServiceFactory;
    private readonly IOptions<PracticeOptions> _options;

    public MigratePaymentIntegrationCommandHandler(
        IGeneralRepository<PaymentPrice> paymentPriceRepository, 
        IIntegrationServiceFactory integrationServiceFactory,
        IOptions<PracticeOptions> options)
    {
        _integrationServiceFactory = integrationServiceFactory;
        _paymentPriceRepository = paymentPriceRepository;
        _options = options;
    }

    public async Task<ICollection<PaymentPriceMigrationReport>> Handle(MigratePaymentIntegrationCommand request, CancellationToken cancellationToken)
    {
        var integrationService = await _integrationServiceFactory.CreateAsync(_options.Value.WildHealth);

        if (integrationService is not WildHealthHintStripeIntegrationService)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Wrong integration configuration.");
        }
        
        var prices = await _paymentPriceRepository
            .All()
            .IncludePaymentPlan()
            .Where(x => x.IsActive)
            .Where(x=> x.PaymentPeriod.PaymentPlan.PracticeId == _options.Value.WildHealth)
            .IncludeIntegrations<PaymentPrice, PaymentPriceIntegration>()
            .ToArrayAsync(cancellationToken: cancellationToken);
        
        var stripePrices = await integrationService.GetPricesAsync();

        var report = new List<PaymentPriceMigrationReport>();
        foreach (var price in prices)
        {
            report.Add(await UpdatePriceIntegrations(price, stripePrices));
        }

        return report;
    }

    private async Task<PaymentPriceMigrationReport> UpdatePriceIntegrations(PaymentPrice price, ICollection<PaymentPriceIntegrationModel> stripePrices
    )
    {
        var result = new PaymentPriceMigrationReport()
        {
            PlanId = price.PaymentPeriod.PaymentPlanId,
            PlanName = price.GetDisplayName(),
            PriceId = price.GetId(),
            Price = price.Price,
            State = "No changes"
        };

        try
        {
            var integrationId = price.GetIntegrationId(IntegrationVendor.Stripe, IntegrationPurposes.Payment.Id);

            var productId = price.GetIntegrationId(IntegrationVendor.Stripe, IntegrationPurposes.Payment.ProductId);

            result.PriceIntegrationId = integrationId;
            result.ProductIntegrationId = productId;

            if (string.IsNullOrEmpty(integrationId) || !string.IsNullOrEmpty(productId))
            {
                return result;
            }

            var stripePrice = stripePrices.FirstOrDefault(x => x.Id == integrationId);
            if (stripePrice is null)
            {
                result.State = "Stripe price is not found";
                return result;
            }

            var newIntegration = new PaymentPriceIntegration(
                vendor: IntegrationVendor.Stripe,
                purpose: IntegrationPurposes.Payment.ProductId,
                value: stripePrice.ProductId);

            price.Integrations.Add(newIntegration);

            await _paymentPriceRepository.SaveAsync();

            result.State = "Product is has been updated";
            result.ProductIntegrationId = stripePrice.ProductId;

            return result;
        }
        catch (Exception e)
        {
            result.State = "Error: " + e.InnerException;
            return result;
        }
    }
}