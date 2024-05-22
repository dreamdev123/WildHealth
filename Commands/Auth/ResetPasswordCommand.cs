using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Validators;

namespace WildHealth.Application.Commands.Auth
{
    /// <summary>
    /// Represents command for resetting password
    /// </summary>
    public class ResetPasswordCommand : IRequest<bool>, IValidatabe
    {
        public string NewPassword { get; }
        
        public ResetPasswordCommand(string newPassword)
        {
            NewPassword = newPassword;
        }

        #region validation

        private class Validator : AbstractValidator<ResetPasswordCommand>
        {
            public Validator()
            {
                RuleFor(x => x.NewPassword).SetValidator(new PasswordValidator());
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
}