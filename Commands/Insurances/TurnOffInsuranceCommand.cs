using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Insurances;

public class TurnOffInsuranceCommand : IRequest, IValidatabe
{
    public int PatientId { get; }
    
    public TurnOffInsuranceCommand(int patientId)
    {
        PatientId = patientId;
    }

    #region validation

    private class Validator : AbstractValidator<TurnOffInsuranceCommand>
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