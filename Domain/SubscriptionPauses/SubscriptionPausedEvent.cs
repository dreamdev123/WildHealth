using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Payments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Domain.SubscriptionPauses;

public class SubscriptionPausedEvent : INotification
{
    public SubscriptionPause Pause { get; }
    
    public Subscription Subscription { get; }
    
    public SubscriptionPausedEvent(SubscriptionPause pause, Subscription subscription)
    {
        Pause = pause;
        Subscription = subscription;
    }

    #region validation

    private class Validator : AbstractValidator<SubscriptionPausedEvent>
    {
        public Validator()
        {
            RuleFor(x => x.Pause).NotNull();
            RuleFor(x => x.Subscription).NotNull();
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