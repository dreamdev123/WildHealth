using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Domain.SubscriptionPauses;

public class PauseSubscriptionCommand : IRequest<Subscription>, IValidatabe
{
    public int Id { get; }
    
    public DateTime EndDate { get;  }
    
    public PauseSubscriptionCommand(int id, DateTime endDate)
    {
        Id = id;
        EndDate = endDate;
    }

    #region validation
    
    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    
    private class Validator : AbstractValidator<PauseSubscriptionCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.EndDate).GreaterThan(DateTime.UtcNow.Date);
        }
    }

    #endregion
}