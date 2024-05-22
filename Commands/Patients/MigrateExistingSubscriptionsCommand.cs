using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Patients;

public class MigrateExistingSubscriptionsCommand : IRequest, IValidatabe
{
    public int PatientId { get; }
    
    public MigrateExistingSubscriptionsCommand(int patientId)
    {
        PatientId = patientId;
    }
    
    #region validation

    private class Validator : AbstractValidator<MigrateExistingSubscriptionsCommand>
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