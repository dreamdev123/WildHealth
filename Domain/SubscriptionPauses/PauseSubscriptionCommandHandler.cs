using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Domain.SubscriptionPauses;

public class PauseSubscriptionCommandHandler : IRequestHandler<PauseSubscriptionCommand, Subscription>
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly MaterializeFlow _materializeFlow;

    public PauseSubscriptionCommandHandler(
        ISubscriptionService subscriptionService, 
        IDateTimeProvider dateTimeProvider, 
        MaterializeFlow materializeFlow)
    {
        _subscriptionService = subscriptionService;
        _dateTimeProvider = dateTimeProvider;
        _materializeFlow = materializeFlow;
    }

    public async Task<Subscription> Handle(PauseSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow();
        
        var subscription = await _subscriptionService.GetAsync(command.Id);

        var createPauseFlow = new PauseSubscriptionFlow(
            Subscription: subscription,
            EndDate: command.EndDate,
            UtcNow: utcNow
        );

        var pause = await createPauseFlow
            .Materialize(_materializeFlow)
            .Select<SubscriptionPause>();

        var processPauseFlow = new ProcessSubscriptionPauseFlow(
            Pause: pause,
            Subscription: subscription,
            UtcNow: utcNow
        );
        
        await processPauseFlow.Materialize(_materializeFlow);

        return subscription;
    }
}