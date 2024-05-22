using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.PromoCodes;
using WildHealth.Domain.Enums.Payments;

namespace WildHealth.Application.CommandHandlers.Subscriptions.Flows;

public class UpdateRenewalStrategyFlow : IMaterialisableFlow
{
    private readonly RenewalStrategy _renewalStrategy;
    private readonly PaymentPrice _paymentPrice;
    private readonly PromoCodeCoupon? _promoCode;
    private readonly EmployerProduct? _employerProduct;

    public UpdateRenewalStrategyFlow(
        RenewalStrategy renewalStrategy, 
        PaymentPrice paymentPrice, 
        PromoCodeCoupon? promoCode, 
        EmployerProduct? employerProduct)
    {
        _renewalStrategy = renewalStrategy;
        _paymentPrice = paymentPrice;
        _promoCode = promoCode;
        _employerProduct = employerProduct;
    }

    public MaterialisableFlowResult Execute()
    {
        _renewalStrategy.PromoCodeId = _promoCode?.GetId();
        _renewalStrategy.PaymentPriceId = _paymentPrice.GetId();
        _renewalStrategy.EmployerProductId = _employerProduct?.GetId();
        _renewalStrategy.Source = RenewalStrategySource.Manual;
        
        return  _renewalStrategy.Updated().ToFlowResult();
    }
}