using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Auth;
using MediatR;

namespace WildHealth.Application.Commands.Auth
{
    public class AuthenticateAfterCheckoutCommand : IRequest<AuthenticationResultModel>, IValidatabe
    {
        public int PracticeId { get; }

        public string Email { get; }
        
        public AuthenticateAfterCheckoutCommand(
            string email,
            int practiceId)
        {
            Email = email;
            PracticeId = practiceId;
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
        
        private class Validator : AbstractValidator<AuthenticateAfterCheckoutCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
                RuleFor(x => x.PracticeId).GreaterThan(0);
            }
        }
        
        #endregion
    }
}