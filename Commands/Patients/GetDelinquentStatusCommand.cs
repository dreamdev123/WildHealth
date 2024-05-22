using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Patients;

namespace WildHealth.Application.Commands.Patients;

public class GetDelinquentStatusCommand: IRequest<DelinquentStatusModel[]>, IValidatabe
{
    public int PatientId { get; }

    public GetDelinquentStatusCommand(int patientId)
    {
        PatientId = patientId;
    }
    
    #region validation

    private class Validator : AbstractValidator<GetDelinquentStatusCommand>
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