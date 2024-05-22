using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Conversations;

public class DeleteReactionCommand : IRequest<Unit>, IValidatabe
{
    public string MessageId { get; }
    
    public string ConversationId { get; }
    
    public string ReactionId { get; }
    
    public DeleteReactionCommand(string messageId, string conversationId, string reactionId)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        ReactionId = reactionId;
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

    private class Validator : AbstractValidator<DeleteReactionCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationId).NotNull().NotEmpty();
            RuleFor(x => x.MessageId).NotNull().NotEmpty();
            RuleFor(x => x.ReactionId).NotNull().NotEmpty();
        }
    }

    #endregion
}