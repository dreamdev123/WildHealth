using WildHealth.Application.Commands._Base;
using WildHealth.Common.Validators;
using WildHealth.Domain.Entities.Users;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Auth;

public class SetupPreauthorizePasswordCommand : IRequest<User>, IValidatabe
{
    public string PreauthorizeRequestToken { get; }
        
    public string Password { get; }

    public SetupPreauthorizePasswordCommand(
        string preauthorizeRequestToken, 
        string password)
    {
        PreauthorizeRequestToken = preauthorizeRequestToken;
        Password = password;
    }

    #region validation

    private class Validator : AbstractValidator<SetupPreauthorizePasswordCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PreauthorizeRequestToken).NotNull().NotEmpty();

            RuleFor(x => x.Password).SetValidator(new PasswordValidator());
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