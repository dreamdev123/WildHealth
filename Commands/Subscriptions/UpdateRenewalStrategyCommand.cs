using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Payments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Subscriptions;

public class UpdateRenewalStrategyCommand : IRequest<RenewalStrategy>, IValidatabe
{
    public int SubscriptionId { get; }
    
    public int PaymentPriceId { get; }
    
    public int? PromoCodeId { get; }
    
    public int? EmployerProductId { get; }
    
    public UpdateRenewalStrategyCommand(
        int subscriptionId, 
        int paymentPriceId, 
        int? promoCodeId, 
        int? employerProductId)
    {
        SubscriptionId = subscriptionId;
        PaymentPriceId = paymentPriceId;
        PromoCodeId = promoCodeId;
        EmployerProductId = employerProductId;
    }
    
    #region validation

    private class Validator : AbstractValidator<UpdateRenewalStrategyCommand>
    {
        public Validator()
        {
            RuleFor(x => x.SubscriptionId).GreaterThan(0);
            RuleFor(x => x.PaymentPriceId).GreaterThan(0);
            RuleFor(x => x.PromoCodeId)
                .GreaterThan(0)
                .When(x => x.PromoCodeId.HasValue);
                
            RuleFor(x => x.EmployerProductId)
                .GreaterThan(0)
                .When(x => x.EmployerProductId.HasValue);
        }
    }

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    #endregion
}