using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Insurances;

public class UpdateFhirPatientCommand : IRequest, IValidatabe
{
    public int PatientId { get; }

    public UpdateFhirPatientCommand(int patientId)
    {
        PatientId = patientId;
    }

    #region validation

    private class Validator : AbstractValidator<UpdateFhirPatientCommand>
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