using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Auth;
using MediatR;

namespace WildHealth.Application.Commands.Auth
{
    public class AuthenticateOnBehalfCommand : IRequest<AuthenticationResultModel>, IValidatabe
    {
        public int PracticeId { get; }
        
        public int? LocationId { get; }
        
        public AuthenticateOnBehalfCommand(
            int practiceId,
            int? locationId)
        {
            PracticeId = practiceId;
            LocationId = locationId;
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

        private class Validator : AbstractValidator<AuthenticateOnBehalfCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.LocationId)
                    .GreaterThan(0)
                    .When(x => x.LocationId.HasValue);
            }
        }
        
        #endregion
    }
}