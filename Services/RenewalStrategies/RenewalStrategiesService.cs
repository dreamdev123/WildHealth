using System.Threading.Tasks;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.RenewalStrategies;

public class RenewalStrategiesService : IRenewalStrategiesService
{
    private readonly IGeneralRepository<RenewalStrategy> _renewalStrategies;

    public RenewalStrategiesService(IGeneralRepository<RenewalStrategy> renewalStrategies)
    {
        _renewalStrategies = renewalStrategies;
    }

    public async Task<RenewalStrategy> SelectAsync(int subscriptionId)
    {
        var renewalStrategy = await _renewalStrategies
            .All()
            .FindAsync(x => x.SubscriptionId == subscriptionId);

        return renewalStrategy;
    }
}