using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Auth;
using MediatR;

namespace WildHealth.Application.Commands.Auth
{
    public class ReauthenticateCommand : IRequest<AuthenticationResultModel>, IValidatabe
    {
        public int? LocationId { get; }
        
        public ReauthenticateCommand(int? locationId)
        {
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

        private class Validator : AbstractValidator<ReauthenticateCommand>
        {
            public Validator()
            {
                RuleFor(x => x.LocationId)
                    .GreaterThan(0)
                    .When(x => x.LocationId.HasValue);
            }
        }
        
        #endregion
    }
}