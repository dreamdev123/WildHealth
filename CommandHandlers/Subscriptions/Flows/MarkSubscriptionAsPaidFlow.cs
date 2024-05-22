using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Integrations;

namespace WildHealth.Application.CommandHandlers.Subscriptions.Flows;

public class MarkSubscriptionAsPaidFlow : IMaterialisableFlow
{
    private readonly Subscription? _subscription;
    private readonly string _integrationId;
    private readonly IntegrationVendor _vendor;

    public MarkSubscriptionAsPaidFlow(Subscription? subscription, string integrationId, IntegrationVendor vendor)
    {
        _subscription = subscription;
        _integrationId = integrationId;
        _vendor = vendor;
    }

    public MaterialisableFlowResult Execute()
    {
        if (_subscription is null) return MaterialisableFlowResult.Empty;
        
        _subscription.MarksAsPaid(_integrationId, _vendor);
        return _subscription.Updated().ToFlowResult();
    }
}