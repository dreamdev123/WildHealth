using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Shortcuts;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Shortcuts;

public class UpdateShortcutCommand : IRequest<Shortcut>, IValidatabe
{
    public int Id { get; }
    
    public string Name { get; }
        
    public string DisplayName { get; }
        
    public string Content { get; }
        
    public int GroupId { get; }
    
    public int EmployeeId { get; }
    
    public int PracticeId { get; }
    
    public UpdateShortcutCommand(
        int id,
        string name, 
        string displayName, 
        string content, 
        int groupId, 
        int employeeId, 
        int practiceId)
    {
        Id = id;
        Name = name;
        DisplayName = displayName;
        Content = content;
        GroupId = groupId;
        EmployeeId = employeeId;
        PracticeId = practiceId;
    }
   
    
    #region validation

    private class Validator : AbstractValidator<UpdateShortcutCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.GroupId).GreaterThan(0);
            RuleFor(x => x.EmployeeId).GreaterThan(0);
            RuleFor(x => x.PracticeId).GreaterThan(0);
                
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(100);
                
            RuleFor(x => x.DisplayName)
                .NotNull()
                .NotEmpty()
                .MaximumLength(100);
                
            RuleFor(x => x.Content)
                .NotNull()
                .NotEmpty()
                .MaximumLength(2500);
                
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