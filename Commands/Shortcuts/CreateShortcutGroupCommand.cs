using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Shortcuts;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Shortcuts;

public class CreateShortcutGroupCommand : IRequest<ShortcutGroup>, IValidatabe
{
    public string Name { get; }

    public string DisplayName { get; }
    
    public bool IsPersonal { get; }
    
    public int EmployeeId { get; }
    
    public int PracticeId { get; }
    
    public CreateShortcutGroupCommand(
        string name, 
        string displayName, 
        bool isPersonal, 
        int employeeId, 
        int practiceId)
    {
        Name = name;
        DisplayName = displayName;
        IsPersonal = isPersonal;
        EmployeeId = employeeId;
        PracticeId = practiceId;
    }
    
    #region validation

    private class Validator : AbstractValidator<CreateShortcutGroupCommand>
    {
        public Validator()
        {
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