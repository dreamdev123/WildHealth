using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Commands.Users;

public record CreateLearnerFromUserCommand(User User) : IRequest, IValidatabe
{
    #region validation
    private class Validator : AbstractValidator<CreateLearnerFromUserCommand>
    {
        public Validator()
        {
            RuleFor(x => x.User).NotNull();
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