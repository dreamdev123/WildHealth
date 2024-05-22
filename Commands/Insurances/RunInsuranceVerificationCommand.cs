using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Insurances;

namespace WildHealth.Application.Commands.Insurances;

public class RunInsuranceVerificationCommand : IRequest<InsuranceVerification[]>, IValidatabe
{
    public int PatientId { get; set; }

    public RunInsuranceVerificationCommand(int patientId)
    {
        PatientId = patientId;
    }
    
    #region validation

    private class Validator : AbstractValidator<RunInsuranceVerificationCommand>
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