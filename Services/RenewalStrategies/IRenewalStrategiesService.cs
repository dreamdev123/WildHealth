using System.Threading.Tasks;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Services.RenewalStrategies;

public interface IRenewalStrategiesService
{
    Task<RenewalStrategy> SelectAsync(int subscriptionId);
}