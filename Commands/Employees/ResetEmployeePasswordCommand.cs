using MediatR;
using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Validators;

namespace WildHealth.Application.Commands.Employees
{
    public class ResetEmployeePasswordCommand : IRequest, IValidatabe
    {
        public int EmployeeId { get; }

        public string NewPassword { get; }

        public ResetEmployeePasswordCommand(int employeeId, string newPassword)
        {
            EmployeeId = employeeId;
            NewPassword = newPassword;
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

        private class Validator : AbstractValidator<ResetEmployeePasswordCommand>
        {
            public Validator()
            {
                RuleFor(x => x.EmployeeId).GreaterThan(0);
                RuleFor(x => x.NewPassword).SetValidator(new PasswordValidator());
            }
        }

        #endregion
    }
}