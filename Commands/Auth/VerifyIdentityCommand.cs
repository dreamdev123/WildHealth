using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Auth
{
    /// <summary>
    /// Represents command for verify identity
    /// </summary>
    public class VerifyIdentityCommand : IRequest<bool>, IValidatabe
    {
        public string Code { get; }
        
        public int UserId { get; }
        
        public VerifyIdentityCommand(
            string code, 
            int userId)
        {
            Code = code;
            UserId = userId;
        }
        
        #region validation

        private class Validator : AbstractValidator<VerifyIdentityCommand>
        {
            public Validator()
            {
                RuleFor(x => x.UserId).GreaterThan(0);
                RuleFor(x => x.Code).NotNull().NotEmpty();
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