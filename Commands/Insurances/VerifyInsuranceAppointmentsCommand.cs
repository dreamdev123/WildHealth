using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Insurances;

public class VerifyInsuranceAppointmentsCommand : IRequest, IValidatabe
{
    public int PracticeId { get; }

    public VerifyInsuranceAppointmentsCommand(int practiceId)
    {
        PracticeId = practiceId;
    }
    
    #region validation

    private class Validator : AbstractValidator<VerifyInsuranceAppointmentsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PracticeId).GreaterThan(0);
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