using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Auth;

public class CheckIfEmailInUseCommand : IRequest<bool>, IValidatabe
{
    public string Email { get; }
    
    public CheckIfEmailInUseCommand(string email)
    {
        Email = email;
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

    private class Validator : AbstractValidator<CheckIfEmailInUseCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
        }
    }
        
    #endregion
}