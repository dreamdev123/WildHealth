using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;
using WildHealth.Twilio.Clients.Enums;

namespace WildHealth.Application.Commands.Conversations;

public class DeleteMessageCommand : IRequest<Unit>, IValidatabe
{
    public string MessageId { get; }
    
    public string ConversationId { get; }
    
    public DeleteMessageReason Reason { get; }
    
    public DeleteMessageCommand(
        string messageId, 
        string conversationId, 
        DeleteMessageReason reason)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        Reason = reason;
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

    private class Validator : AbstractValidator<DeleteMessageCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationId).NotNull().NotEmpty();
            RuleFor(x => x.MessageId).NotNull().NotEmpty();
            RuleFor(x => x.Reason).IsInEnum();
        }
    }

    #endregion
}