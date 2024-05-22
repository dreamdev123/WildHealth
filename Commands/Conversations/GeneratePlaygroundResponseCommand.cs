using WildHealth.Application.Commands._Base;
using WildHealth.Common.Enums;
using WildHealth.Jenny.Clients.Models;
using FluentValidation;
using MediatR;
using WildHealth.Domain.Enums.Conversations;

namespace WildHealth.Application.Commands.Conversations;

public class GeneratePlaygroundResponseCommand : IRequest<TextCompletionResponseModel>, IValidatabe
{
    public ConversationType Type { get; }
    public string ConversationSid { get; }    
    public string MessageSid { get; }
    public FlowType FlowType { get; }
    public string UserUniversalId { get; }
    
    public GeneratePlaygroundResponseCommand(ConversationType type, string conversationSid, string messageSid, FlowType flowType, string userUniversalId = "")
    {
        Type = type;
        ConversationSid = conversationSid;
        MessageSid = messageSid;
        FlowType = flowType;
        UserUniversalId = userUniversalId;
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
    
    private class Validator : AbstractValidator<GeneratePlaygroundResponseCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.ConversationSid).NotEmpty();
            RuleFor(x => x.MessageSid).NotEmpty();
            RuleFor(x => x.FlowType).NotNull();
        }
    }
    
    #endregion
}