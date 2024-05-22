using WildHealth.Application.Commands._Base;
using WildHealth.Twilio.Clients.Enums;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Conversations;

public class AddReactionCommand : IRequest<Unit>, IValidatabe
{
    public string MessageId { get; }
    
    public string ConversationId { get; }
    
    public string ParticipantId { get; }
    
    public ReactionType Type { get; }
    
    public AddReactionCommand(string messageId, string conversationId, string participantId, ReactionType type)
    {
        MessageId = messageId;
        ConversationId = conversationId;
        ParticipantId = participantId;
        Type = type;
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

    private class Validator : AbstractValidator<AddReactionCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationId).NotNull().NotEmpty();
            RuleFor(x => x.MessageId).NotNull().NotEmpty();
            RuleFor(x => x.ParticipantId).NotNull().NotEmpty();
            RuleFor(x => x.Type).IsInEnum();
        }
    }

    #endregion
}