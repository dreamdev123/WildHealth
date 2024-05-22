using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.PaymentPrices;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Domain.Models.Extensions;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Subscriptions;

public class OverwriteSubscriptionsCommandHandler : IRequestHandler<OverwriteSubscriptionsCommand>
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPaymentPriceService _paymentPriceService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly OverwriteSubscriptionsOptions _options;
    private readonly MaterializeFlow _materialize;
    private readonly ILogger _logger;

    public OverwriteSubscriptionsCommandHandler(
        ISubscriptionService subscriptionService, 
        IPaymentPriceService paymentPriceService, 
        IDateTimeProvider dateTimeProvider, 
        IOptions<OverwriteSubscriptionsOptions> options, 
        MaterializeFlow materialize, 
        ILogger<OverwriteSubscriptionsCommandHandler> logger)
    {
        _subscriptionService = subscriptionService;
        _paymentPriceService = paymentPriceService;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
        _materialize = materialize;
        _logger = logger;
    }

    public async Task Handle(OverwriteSubscriptionsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Started overwriting subscriptions");

        if (!_options.IsEnabled)
        {
            _logger.LogInformation("Skipped overwriting subscriptions");
            
            return;
        }
        
        var now = _dateTimeProvider.UtcNow();
        var endDate = now.AddDays(_options.NoticePeriodInDays);
        var altFullPaymentPrice = await _paymentPriceService.GetAsync(_options.AlternativeFullPriceId);
        var altPartialPaymentPrice = await _paymentPriceService.GetAsync(_options.AlternativePartialPriceId);
        var subscriptions = (await _subscriptionService.GetFinishingSubscriptionsAsync(endDate, _options.PracticeId)).ToArray();
        
        // Since our finishing subscriptions operates on a rolling 7 day window, we want to make sure when we initially release that we only grab those that are 30 days
        // from renewal, since we are releasing on July 5, this means the earliest subscription EndDate will be Aug 4
        var subscriptionsFilters = subscriptions.Where(o => o.EndDate >= DateTime.Parse("2023-08-04"));

        _logger.LogInformation("Found {subscriptionsCount} subscriptions for overwriting", subscriptions.Length);
        
        foreach (var subscription in subscriptionsFilters)
        {
            _logger.LogInformation("Started overwriting subscription with Id = {subscriptionId} for patient with Id = {patientId}", subscription.GetId(), subscription.PatientId);
            
            var flow = new OverwriteSubscriptionFlow(
                subscription: subscription,
                altFullPaymentPrice: altFullPaymentPrice,
                altPartialPaymentPrice: altPartialPaymentPrice,
                minPriceCriteria: _options.MinPriceCriteria,
                noticePeriod: _options.NoticePeriodInDays,
                now: now
            );

            var result = await flow.Materialize(_materialize).ToTry();

            result.DoIfError(error => _logger.LogError( "Error during overwriting subscription with Id = {subscriptionId} for patient with Id = {patientId}. {error}", subscription.GetId(), subscription.PatientId, error));

            if (result.IsSuccess())
            {
                _logger.LogInformation("Finished overwriting subscription with Id = {subscriptionId} for patient with Id = {patientId}", subscription.GetId(), subscription.PatientId);
            }
        }
    }
}