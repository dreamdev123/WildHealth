using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Auth;
using WildHealth.Domain.Enums.User;
using Microsoft.AspNetCore.Http;
using MediatR;

namespace WildHealth.Application.Commands.Auth
{
    public class AuthenticateWithAuth2Command : IRequest<AuthenticationResultModel>, IValidatabe
    {
        public string Code { get; }
        
        public HttpContext Context { get; }
        
        public AuthorizationProvider Provider { get; }
        
        public AuthenticateWithAuth2Command(
            string code,
            HttpContext context,
            AuthorizationProvider provider)
        {
            Code = code;
            Context = context;
            Provider = provider;
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

        private class Validator : AbstractValidator<AuthenticateWithAuth2Command>
        {
            public Validator()
            {
                RuleFor(x => x.Code).NotNull().NotEmpty();
                RuleFor(x => x.Context).NotNull();
                RuleFor(x => x.Provider).IsInEnum();
            }
        }
        
        #endregion
    }
}