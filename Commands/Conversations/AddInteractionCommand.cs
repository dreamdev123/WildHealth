using WildHealth.Application.Commands._Base;
using WildHealth.Twilio.Clients.Enums;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Conversations;

public class AddInteractionCommand : IRequest<Unit>, IValidatabe
{
    public string ConversationId { get; }
    
    public string MessageId { get; }
    
    public string ReferenceId { get; }
    
    public string ParticipantId { get; }
    
    public string Detail { get; }
    
    public InteractionType Type { get; }
    public string UserUniversalId { get; }
    
    public AddInteractionCommand(
    string conversationId, 
    string messageId, 
    string referenceId, 
    string participantId, 
    string detail, 
    InteractionType type,
    string userUniversalId)
    {
        ConversationId = conversationId;
        MessageId = messageId;
        ReferenceId = referenceId;
        ParticipantId = participantId;
        Detail = detail;
        Type = type;
        UserUniversalId = userUniversalId;
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

    private class Validator : AbstractValidator<AddInteractionCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationId).NotNull().NotEmpty();
            RuleFor(x => x.MessageId).NotNull().NotEmpty();
            RuleFor(x => x.ReferenceId).NotNull().NotEmpty();
            RuleFor(x => x.ParticipantId).NotNull().NotEmpty();
            RuleFor(x => x.Type).IsInEnum();
            // UserUniversalId may be null if interaction is coming from AI
        }
    }

    #endregion
}