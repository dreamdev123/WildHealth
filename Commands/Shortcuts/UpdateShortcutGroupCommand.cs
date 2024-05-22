using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Shortcuts;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Shortcuts;

public class UpdateShortcutGroupCommand : IRequest<ShortcutGroup>, IValidatabe
{
    public int Id { get; }
    
    public string Name { get; }

    public string DisplayName { get; }
    
    public int EmployeeId { get; }
    
    public int PracticeId { get; }
    
    public UpdateShortcutGroupCommand(
        int id,
        string name, 
        string displayName,
        int employeeId, 
        int practiceId)
    {
        Id = id;
        Name = name;
        DisplayName = displayName;
        EmployeeId = employeeId;
        PracticeId = practiceId;
    }
    
    #region validation

    private class Validator : AbstractValidator<UpdateShortcutGroupCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Name).NotNull().NotEmpty();
            RuleFor(x => x.DisplayName).NotNull().NotEmpty();
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