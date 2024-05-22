using MediatR;

namespace WildHealth.Application.Commands.Subscriptions
{
    public class RefreshSubscriptionIntegrationCommand : IRequest
    {
        public int SubscriptionId { get; }

        public RefreshSubscriptionIntegrationCommand(int subscriptionId)
        {
            SubscriptionId = subscriptionId;
        }
    }
}