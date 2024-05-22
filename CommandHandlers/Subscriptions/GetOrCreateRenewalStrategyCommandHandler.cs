using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Services.RenewalStrategies;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Application.Materialization;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Subscriptions;

public class GetOrCreateRenewalStrategyCommandHandler : IRequestHandler<GetOrCreateRenewalStrategyCommand, RenewalStrategy>
{
    private readonly IRenewalStrategiesService _renewalStrategiesService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly MaterializeFlow _materialization;

    public GetOrCreateRenewalStrategyCommandHandler(
        IRenewalStrategiesService renewalStrategiesService, 
        ISubscriptionService subscriptionService,
        MaterializeFlow materialization)
    {
        _renewalStrategiesService = renewalStrategiesService;
        _subscriptionService = subscriptionService;
        _materialization = materialization;
    }

    public async Task<RenewalStrategy> Handle(GetOrCreateRenewalStrategyCommand command, CancellationToken cancellationToken)
    {
        var strategy = await _renewalStrategiesService.SelectAsync(command.SubscriptionId).ToOption();

        if (strategy.HasValue())
        {
            return strategy.Value();
        }

        var subscription = await _subscriptionService.GetAsync(command.SubscriptionId);

        var flow = new CreateRenewalStrategyFlow(subscription);
        
        return await flow.Materialize(_materialization).Select<RenewalStrategy>();
    }
}