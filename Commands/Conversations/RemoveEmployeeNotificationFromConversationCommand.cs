using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Conversations;

public class RemoveEmployeeNotificationFromConversationCommand : IRequest,IValidatabe
{
    public int ConversationId { get; }
    public int UserId { get; }

    public RemoveEmployeeNotificationFromConversationCommand(
        int conversationId,
        int userId)
    {
        ConversationId = conversationId;
        UserId = userId;
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

    private class Validator : AbstractValidator<RemoveEmployeeNotificationFromConversationCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationId).GreaterThan(0);
            RuleFor(x => x.UserId).GreaterThan(0);
        }
    }

    #endregion
}