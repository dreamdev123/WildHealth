using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.PaymentPrices;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Options;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Application.Services.CallToActions;
using Microsoft.Extensions.Options;
using MediatR;

namespace WildHealth.Application.Domain.Actions;

public class OfferStandardPlanUpSellCommandHandler : IRequestHandler<OfferStandardPlanUpSellCommand>
{
    private readonly ICallToActionsService _callToActionsService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPaymentPriceService _paymentPriceService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly OfferStandardPlanUpSellOptions _options;
    private readonly MaterializeFlow _materialize;
    private readonly ILogger _logger;

    public OfferStandardPlanUpSellCommandHandler(
        ICallToActionsService callToActionsService,
        ISubscriptionService subscriptionService, 
        IPaymentPriceService paymentPriceService, 
        IDateTimeProvider dateTimeProvider, 
        IOptions<OfferStandardPlanUpSellOptions> options, 
        MaterializeFlow materialize, 
        ILogger<OfferStandardPlanUpSellCommandHandler> logger)
    {
        _callToActionsService = callToActionsService;
        _subscriptionService = subscriptionService;
        _paymentPriceService = paymentPriceService;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
        _materialize = materialize;
        _logger = logger;
    }

    public async Task Handle(OfferStandardPlanUpSellCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Started offering Standard Plan Up Sell");

        if (!_options.IsEnabled)
        {
            _logger.LogInformation("Skipped offering Standard Plan Up Sell");
            
            return;
        }
        
        var now = _dateTimeProvider.UtcNow();
        var endDate = now.Date.AddDays(_options.NoticePeriodInDays).Date;
        var startDate = now.Date.AddMonths(1).AddDays(1).Date;
        var altFullPaymentPrice = await _paymentPriceService.GetAsync(_options.AlternativeFullPriceId);
        var altPartialPaymentPrice = await _paymentPriceService.GetAsync(_options.AlternativePartialPriceId);
        var subscriptions = (await _subscriptionService.GetFinishingSubscriptionsAsync(startDate, endDate, _options.PracticeId)).ToArray();
        
        _logger.LogInformation("Found {subscriptionsCount} subscriptions for offering", subscriptions.Length);
        
        foreach (var subscription in subscriptions)
        {
            _logger.LogInformation("Started offering Standard Plan Up Sell for patient with Id = {patientId}", subscription.PatientId);

            var callToActions = await _callToActionsService.AllAsync(subscription.PatientId);
            
            var flow = new OfferStandardPlanUpSellFlow(
                Subscription: subscription,
                AltFullPaymentPrice: altFullPaymentPrice,
                AltPartialPaymentPrice: altPartialPaymentPrice,
                MinPriceCriteria: _options.MinPriceCriteria,
                CallToActions: callToActions
            );

            var result = await flow.Materialize(_materialize).ToTry();

            result.DoIfError(error => _logger.LogError( "Error during offering Standard Plan Up Sell for patient with Id = {patientId}. {error}", subscription.PatientId, error));

            if (result.IsSuccess())
            {
                _logger.LogInformation("Finished offering Standard Plan Up Sell for patient with Id = {patientId}", subscription.PatientId);
            }
        }
    }
}