using WildHealth.Application.Commands._Base;
using WildHealth.Jenny.Clients.Models;
using FluentValidation;
using MediatR;
using WildHealth.Common.Enums;

namespace WildHealth.Application.Commands.Ai;

public class ConversationAiHcAssistCommand : IRequest<TextCompletionResponseModel>, IValidatabe
{
    public string ConversationSid { get; }    
    public string MessageSid { get; }
    public FlowType FlowType { get; }
    
    public ConversationAiHcAssistCommand(string conversationSid, string messageSid, FlowType flowType)
    {
        ConversationSid = conversationSid;
        MessageSid = messageSid;
        FlowType = flowType;
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
    
    private class Validator : AbstractValidator<ConversationAiHcAssistCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationSid).NotEmpty();
            RuleFor(x => x.MessageSid).NotEmpty();
            RuleFor(x => x.FlowType).NotNull();
        }
    }
    
    #endregion
}