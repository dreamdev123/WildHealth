using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Domain.SubscriptionPauses;

public class ResumeSubscriptionCommandHandler : IRequestHandler<ResumeSubscriptionCommand, Subscription>
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly MaterializeFlow _materializeFlow;

    public ResumeSubscriptionCommandHandler(
        ISubscriptionService subscriptionService, 
        IDateTimeProvider dateTimeProvider,
        MaterializeFlow materializeFlow)
    {
        _subscriptionService = subscriptionService;
        _dateTimeProvider = dateTimeProvider;
        _materializeFlow = materializeFlow;
    }

    public async Task<Subscription> Handle(ResumeSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow();
        
        var subscription = await _subscriptionService.GetAsync(command.Id);

        var flow = new ResumeSubscriptionFlow(subscription, utcNow);
        
        await flow.Materialize(_materializeFlow);

        return subscription;
    }
}