using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;
using WildHealth.Domain.Entities.Employees;

namespace WildHealth.Application.Commands.Employees;

public class DeleteFellowCommand : IRequest, IValidatabe
{
    public int EmployeeId { get; }

    public DeleteFellowCommand(int employeeId)
    {
        EmployeeId = employeeId;
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

    private class Validator : AbstractValidator<DeleteFellowCommand>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId).GreaterThan(0);
        }
    }

    #endregion
}