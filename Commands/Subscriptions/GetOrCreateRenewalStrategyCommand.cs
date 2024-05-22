using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Payments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Subscriptions;

public class GetOrCreateRenewalStrategyCommand : IRequest<RenewalStrategy>, IValidatabe
{
    public int SubscriptionId { get; }
    
    public GetOrCreateRenewalStrategyCommand(int subscriptionId)
    {
        SubscriptionId = subscriptionId;
    }
    
    #region validation

    private class Validator : AbstractValidator<GetOrCreateRenewalStrategyCommand>
    {
        public Validator()
        {
            RuleFor(x => x.SubscriptionId).GreaterThan(0);
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