using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Auth
{
    /// <summary>
    /// Represents send Identity Verification Command
    /// </summary>
    public class SendIdentityVerificationCommand : IRequest<bool>, IValidatabe
    {
        public int UserId { get; }

        public string NewPhoneNumber { get; }

        public SendIdentityVerificationCommand(int userId, string newPhoneNumber)
        {
            UserId = userId;
            NewPhoneNumber = newPhoneNumber;
        }

        #region validation

        private class Validator : AbstractValidator<SendIdentityVerificationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.UserId).GreaterThan(0);
                RuleFor(x => x.NewPhoneNumber).NotNull().NotEmpty();
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