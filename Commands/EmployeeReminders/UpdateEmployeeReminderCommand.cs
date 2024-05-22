using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.EmployeeReminders;

namespace WildHealth.Application.Commands.EmployeeReminders;

public class UpdateEmployeeReminderCommand : IRequest<EmployeeReminder>, IValidatabe
{
    public int Id { get; }

    public DateTime DateRemind { get; }
    
    public string Title { get; }
    
    public string Description { get; }

    public UpdateEmployeeReminderCommand(int id, DateTime dateRemind, string title, string description)
    {
        Id = id;
        DateRemind = dateRemind;
        Title = title;
        Description = description;
    }
    
    #region validation

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<UpdateEmployeeReminderCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Title.Length).GreaterThan(0).LessThan(500);
            RuleFor(x => x.Description.Length).LessThan(500);
        }
    }

    #endregion
}