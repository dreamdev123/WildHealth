using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Insurances;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Insurances;

public class DeactivateCoverageCommand : IRequest<Coverage>, IValidatabe
{
    public int Id { get; }
    
    public DeactivateCoverageCommand(int id)
    {
        Id = id;
    }

    #region validation
    
    private class Validator : AbstractValidator<DeactivateCoverageCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
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