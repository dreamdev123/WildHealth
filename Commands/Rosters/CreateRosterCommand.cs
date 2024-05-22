using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Employees;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Rosters;

public class CreateRosterCommand : IRequest<Roster>, IValidatabe
{
    public string Name { get; }
    
    public CreateRosterCommand(string name)
    {
        Name = name;
    }
    
    #region private

    private class Validator : AbstractValidator<CreateRosterCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotNull().NotEmpty().MaximumLength(100);
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