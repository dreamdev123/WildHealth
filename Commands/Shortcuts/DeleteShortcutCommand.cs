using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Shortcuts;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Shortcuts;

public class DeleteShortcutCommand : IRequest<Shortcut>, IValidatabe
{
    public int Id { get; }
    
    public int EmployeeId { get; }
    
    public int PracticeId { get; }
    
    public DeleteShortcutCommand(int id, int employeeId, int practiceId)
    {
        Id = id;
        EmployeeId = employeeId;
        PracticeId = practiceId;
    }
    
    #region validation

    private class Validator : AbstractValidator<DeleteShortcutCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.EmployeeId).GreaterThan(0);
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