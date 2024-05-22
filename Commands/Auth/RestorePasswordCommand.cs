using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Validators;
using MediatR;
using WildHealth.Domain.Enums.User;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Commands.Auth
{
    public class RestorePasswordCommand : IRequest<User>, IValidatabe
    {
        public string Code { get; }
        
        public string Password { get; }
        public ConfirmCodeType ConfirmCodeType { get; }

        public RestorePasswordCommand(
            string code, 
            string password,
            ConfirmCodeType confirmCodeType)
        {
            Code = code;
            Password = password;
            ConfirmCodeType = confirmCodeType;
        }

        #region validation

        private class Validator : AbstractValidator<RestorePasswordCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Code).NotNull().NotEmpty();

                RuleFor(x => x.Password).SetValidator(new PasswordValidator());

                RuleFor(x => x.ConfirmCodeType).IsInEnum();
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