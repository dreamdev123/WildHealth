using System;
using System.Collections.Generic;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.EmployeeReminders;

namespace WildHealth.Application.Commands.EmployeeReminders;

public class GetEmployeeReminderCommand : IRequest<ICollection<EmployeeReminder>>, IValidatabe
{
    public int EmployeeId { get; }

    public DateTime Date { get; }

    public GetEmployeeReminderCommand(int employeeId, DateTime date)
    {
        EmployeeId = employeeId;
        Date = date;
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

    private class Validator : AbstractValidator<GetEmployeeReminderCommand>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId).GreaterThan(0);
        }
    }

    #endregion
}