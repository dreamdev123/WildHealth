using MediatR;
using FluentValidation;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Employees
{
    public class SendEmployeeInviteCommand : IRequest, IValidatabe
    {
        public int EmployeeId { get; }

        public SendEmployeeInviteCommand(int employeeId)
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

    private class Validator : AbstractValidator<SendEmployeeInviteCommand>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId).GreaterThan(0);
        }
    }

    #endregion
}
}