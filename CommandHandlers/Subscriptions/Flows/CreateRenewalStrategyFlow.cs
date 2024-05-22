using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.CommandHandlers.Subscriptions.Flows;

public class CreateRenewalStrategyFlow : IMaterialisableFlow
{
    private readonly Subscription _subscription;

    public CreateRenewalStrategyFlow(Subscription subscription)
    {
        _subscription = subscription;
    }
    
    public MaterialisableFlowResult Execute()
    {
        var strategy = new RenewalStrategy(
            subscription: _subscription,
            paymentPriceId: _subscription.PaymentPrice.GetId(),
            promoCodeId: _subscription.PromoCodeCoupon?.GetId(),
            employerProductId: _subscription.EmployerProduct?.GetId()
        );

        return strategy.Added().ToFlowResult();
    }
}