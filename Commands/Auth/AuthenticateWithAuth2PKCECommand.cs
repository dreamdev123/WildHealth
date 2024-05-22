using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Auth;
using WildHealth.Domain.Enums.User;

namespace WildHealth.Application.Commands.Auth;

public class AuthenticateWithAuth2PKCECommand : IRequest<AuthenticationResultModel>, IValidatabe
{
    public string Code { get; }
        
    public HttpContext Context { get; }
        
    public AuthorizationProvider Provider { get; }
        
    public string CodeVerifier { get; set; }
    public AuthenticateWithAuth2PKCECommand(
        string code,
        HttpContext context,
        AuthorizationProvider provider,
        string codeVerifier)
    {
        Code = code;
        Context = context;
        Provider = provider;
        CodeVerifier = codeVerifier;
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

    private class Validator : AbstractValidator<AuthenticateWithAuth2PKCECommand>
    {
        public Validator()
        {
            RuleFor(x => x.Code).NotNull().NotEmpty();
            RuleFor(x => x.Context).NotNull();
            RuleFor(x => x.Provider).IsInEnum();
            RuleFor(x => x.CodeVerifier).NotNull().NotEmpty();
        }
    }
        
    #endregion
}