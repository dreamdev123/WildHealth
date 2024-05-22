using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Address;

namespace WildHealth.Application.Commands.Payments;

public class AddPaymentPlanInsuranceStatesCommand : IRequest, IValidatabe
{
    public int PaymentPlanId { get; }
    
    public StateModel[] States { get; }

    public AddPaymentPlanInsuranceStatesCommand(int paymentPlanId, StateModel[] states)
    {
        PaymentPlanId = paymentPlanId;
        States = states;
    }

    #region validation

    private class Validator : AbstractValidator<AddPaymentPlanInsuranceStatesCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PaymentPlanId).GreaterThan(0);
            RuleFor(x => x.States).NotEmpty();
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