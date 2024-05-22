using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations;

public class CheckEmployeeUnreadMessagesCommand : IRequest, IValidatabe
{
    public int PracticeId { get; set; }
    public CheckEmployeeUnreadMessagesCommand(int practiceId)
    {
        PracticeId = practiceId;
    }

    #region validation

    private class Validator : AbstractValidator<CheckEmployeeUnreadMessagesCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PracticeId).GreaterThan(0);
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