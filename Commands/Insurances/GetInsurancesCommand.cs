using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Insurances;

namespace WildHealth.Application.Commands.Insurances;

public class GetInsurancesCommand : IRequest<Insurance[]>, IValidatabe
{
    public int? StateId { get; }
    
    public int? Age { get; }

    public GetInsurancesCommand(int? stateId, int? age)
    {
        StateId = stateId;
        Age = age;
    }

    public GetInsurancesCommand()
    {
        
    }

    #region validation

    private class Validator : AbstractValidator<GetInsurancesCommand>
    {
        public Validator()
        {
            RuleFor(x => x.StateId)
                .GreaterThan(0)
                .When(x => x.StateId.HasValue);
            
            RuleFor(x => x.Age)
                .GreaterThan(0)
                .When(x => x.Age.HasValue);
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