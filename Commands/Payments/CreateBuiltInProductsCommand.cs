using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Payments;

public class CreateBuiltInProductsCommand : IRequest<PatientProduct[]>, IValidatabe
{
    public int SubscriptionId { get; }

    public CreateBuiltInProductsCommand(int subscriptionId)
    {
        SubscriptionId = subscriptionId;
    }
    
    #region validation

    private class Validator : AbstractValidator<CreateBuiltInProductsCommand>
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