using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations;

public class CheckConversationSignOffCommand : IRequest<Unit>, IValidatabe
{
    public int ConversationId { get; }

    public CheckConversationSignOffCommand(int conversationId)
    {
        ConversationId = conversationId;
    }

    #region validation

    private class Validator : AbstractValidator<CheckConversationSignOffCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationId).GreaterThan(0);
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