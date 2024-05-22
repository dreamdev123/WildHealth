using System.Collections.Generic;
using WildHealth.Application.Commands._Base;
using WildHealth.Twilio.Clients.Models.Conversations.Alerts;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Conversations;

public class CreateMessageAlertCommand : IRequest, IValidatabe
{
    public string MessageId { get; }
    
    public string ConversationId { get; }
    
    public MessageAlertType Type { get; }
    
    public List<KeyValuePair<string, string>> Data { get; }
    
    public CreateMessageAlertCommand(
        string messageId, 
        string conversationId, 
        MessageAlertType type, 
        List<KeyValuePair<string, string>> data)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        Type = type;
        Data = data;
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

    private class Validator : AbstractValidator<CreateMessageAlertCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationId).NotNull().NotEmpty();
            RuleFor(x => x.MessageId).NotNull().NotEmpty();
            RuleFor(x => x.Type).IsInEnum();
        }
    }

    #endregion
}