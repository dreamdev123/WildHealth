using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Insurances;

public class GetPatient271Command : IRequest<string>, IValidatabe
{
    public int PatientId { get; }

    public GetPatient271Command(int patientId)
    {
        PatientId = patientId;
    }
    
    #region validation

    private class Validator : AbstractValidator<GetPatient271Command>
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