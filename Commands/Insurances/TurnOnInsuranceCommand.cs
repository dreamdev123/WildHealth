using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Insurances;

public class TurnOnInsuranceCommand: IRequest, IValidatabe
{
    public int PatientId { get; }
    
    public TurnOnInsuranceCommand(int patientId)
    {
        PatientId = patientId;
    }

    #region validation

    private class Validator : AbstractValidator<TurnOnInsuranceCommand>
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