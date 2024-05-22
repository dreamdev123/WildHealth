using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Products;

public class ResetPatientProductsCommand : IRequest, IValidatabe
{
    public int SubscriptionId { get; }

    public ResetPatientProductsCommand(int subscriptionId)
    {
        SubscriptionId = subscriptionId;
    }
    
    #region validation

    private class Validator : AbstractValidator<ResetPatientProductsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.SubscriptionId).NotNull();
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