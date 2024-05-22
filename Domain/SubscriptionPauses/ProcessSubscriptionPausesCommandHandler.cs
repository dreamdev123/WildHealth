using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.SubscriptionPauses;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.Domain.SubscriptionPauses;

public class ProcessSubscriptionPausesCommandHandler : IRequestHandler<ProcessSubscriptionPausesCommand>
{
    private readonly ISubscriptionPausesService _subscriptionPausesService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly MaterializeFlow _materializeFlow;
    private readonly ILogger _logger;

    public ProcessSubscriptionPausesCommandHandler(
        ISubscriptionPausesService subscriptionPausesService,
        ISubscriptionService subscriptionService, 
        IDateTimeProvider dateTimeProvider, 
        MaterializeFlow materializeFlow,
        ILogger<ProcessSubscriptionPausesCommandHandler> logger)
    {
        _subscriptionPausesService = subscriptionPausesService;
        _subscriptionService = subscriptionService;
        _dateTimeProvider = dateTimeProvider;
        _materializeFlow = materializeFlow;
        _logger = logger;
    }

    public async Task Handle(ProcessSubscriptionPausesCommand command, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow();

        var pauses = await _subscriptionPausesService.GetAsync(utcNow);

        foreach (var pause in pauses)
        {
            var result = await ProcessSubscriptionPause(pause, utcNow).ToTry();
            
            result.DoIfError(error => _logger.LogError("Error during processing subscription pause with {id}. {error}", pause.GetId(), error));
        }
    }
    
    #region private

    private async Task<bool> ProcessSubscriptionPause(WildHealth.Domain.Entities.Payments.SubscriptionPause pause, DateTime utcNow)
    {
        var subscription = await _subscriptionService.GetAsync(pause.SubscriptionId);
            
        var flow = new ProcessSubscriptionPauseFlow(
            Pause: pause,
            Subscription: subscription,
            UtcNow: utcNow
        );

        await flow.Materialize(_materializeFlow);

        return true;
    }
    
    #endregion
}