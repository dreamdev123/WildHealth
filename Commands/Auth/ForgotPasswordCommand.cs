using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Auth
{
    public class ForgotPasswordCommand : IRequest, IValidatabe
    {
        public string Email { get; }
        
        public int PracticeId { get; }

        public ForgotPasswordCommand(string email, int practiceId)
        {
            Email = email;
            PracticeId = practiceId;
        }

        #region validation

        private class Validator : AbstractValidator<ForgotPasswordCommand>
        {
            public Validator()
            {

                RuleFor(x => x.Email).EmailAddress();
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
}