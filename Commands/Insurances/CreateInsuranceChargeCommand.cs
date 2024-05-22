using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Insurances;

public class CreateInsuranceChargeCommand : IRequest<Unit>, IValidatabe
{
    public int PatientId { get; }
    
    public decimal Price { get; }
    
    public CreateInsuranceChargeCommand(int patientId, decimal price)
    {
        PatientId = patientId;
        Price = price;
    }

    #region validation

    private class Validator : AbstractValidator<CreateInsuranceChargeCommand>
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
    /// <returns></returns>
    public void Validate() => new Validator().ValidateAndThrow(this);

    #endregion
}