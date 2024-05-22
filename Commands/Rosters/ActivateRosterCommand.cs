using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Employees;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Rosters;

public class ActivateRosterCommand : IRequest<Roster>, IValidatabe
{
    public int Id { get; }
    
    public bool IsActive { get; }
    
    public ActivateRosterCommand(int id, bool isActive)
    {
        Id = id;
        IsActive = isActive;
    }
    
    #region private

    private class Validator : AbstractValidator<ActivateRosterCommand>
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