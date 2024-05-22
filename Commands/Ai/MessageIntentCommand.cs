using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Ai;

public class MessageIntentCommand : IRequest, IValidatabe
{
    public string ConversationSid { get; }    
    public string MessageSid { get; }
    public string UniversalId { get; }
    
    public MessageIntentCommand(
        string conversationSid, 
        string messageSid,
        string universalId)
    {
        ConversationSid = conversationSid;
        MessageSid = messageSid;
        UniversalId = universalId;
    }

    #region Validation
    
    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    /// <returns></returns>
    public void Validate() => new Validator().ValidateAndThrow(this);
    
    private class Validator : AbstractValidator<MessageIntentCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationSid).NotEmpty();
            RuleFor(x => x.UniversalId).NotEmpty();
            RuleFor(x => x.MessageSid).NotEmpty();
        }
    }
    
    #endregion
}