using WildHealth.Application.Commands._Base;
using WildHealth.Twilio.Clients.Models.Conversations.Alerts;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Conversations;

public class ReactOnConversationMessageAlertCommand : IRequest, IValidatabe
{
    public int UserId { get;  }
    
    public string ConversationId { get; }
    
    public string MessageId { get; }
    
    public string AlertId { get; }
    
    public MessageAlertActionType ActionType { get; }
    
    public string Details { get; }
    
    public ReactOnConversationMessageAlertCommand(
        int userId, 
        string conversationId, 
        string messageId, 
        string alertId,
        MessageAlertActionType actionType, 
        string details)
    {
        UserId = userId;
        ConversationId = conversationId;
        MessageId = messageId;
        AlertId = alertId;
        ActionType = actionType;
        Details = details;
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

    private class Validator : AbstractValidator<ReactOnConversationMessageAlertCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationId).NotNull().NotEmpty();
            RuleFor(x => x.MessageId).NotNull().NotEmpty();
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.ActionType).IsInEnum();
        }
    }

    #endregion
}