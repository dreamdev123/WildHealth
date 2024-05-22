using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Auth;

namespace WildHealth.Application.Commands.Auth
{
    public class RefreshTokenCommand : IRequest<AuthenticationResultModel>, IValidatabe
    {
        public string RefreshToken { get; set; }

        public RefreshTokenCommand(string refreshToken)
        {
            RefreshToken = refreshToken;
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

        private class Validator : AbstractValidator<RefreshTokenCommand>
        {
            public Validator()
            {
                RuleFor(x => x.RefreshToken).NotNull().NotEmpty();
            }
        }

        #endregion
    }
}
