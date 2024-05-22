using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Enums.Products;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Products;

public class SubstituteProductsCommand : IRequest, IValidatabe
{
    public int PatientId { get; }
    
    public PaymentFlow OldPaymentFlow { get; }
    
    public PaymentFlow NewPaymentFlow { get; }

    public SubstituteProductsCommand(
        int patientId, 
        PaymentFlow oldPaymentFlow, 
        PaymentFlow newPaymentFlow)
    {
        PatientId = patientId;
        OldPaymentFlow = oldPaymentFlow;
        NewPaymentFlow = newPaymentFlow;
    }
    
    #region validation

    private class Validator : AbstractValidator<SubstituteProductsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
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