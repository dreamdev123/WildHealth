using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Enums.Conversations;

namespace WildHealth.Application.Commands.Conversations;

public class PublishConversationParticipantAddedEventCommand : IRequest, IValidatabe
{
    public string ConversationSid { get; }
    
    public int ConversationId { get; }
    
    public string Subject { get; }
    
    public ConversationState State { get; }
    
    public string ParticipantSid { get; }
    
    public string ParticipantUniversalId { get; }

    public PublishConversationParticipantAddedEventCommand(
        string conversationVendorExternalId,
        int conversationId,
        string subject,
        ConversationState state,
        string participantVendorExternalId,
        string employeeUniversalId)
    {
        ConversationSid = conversationVendorExternalId;
        ConversationId = conversationId;
        Subject = subject;
        State = state;
        ParticipantSid = participantVendorExternalId;
        ParticipantUniversalId = employeeUniversalId;
    }

    #region validation

    private class Validator : AbstractValidator<PublishConversationParticipantAddedEventCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationSid).NotEmpty().NotNull();
            RuleFor(x => x.ConversationId).NotNull();
            RuleFor(x => x.Subject).NotEmpty().NotNull();
            RuleFor(x => x.State).NotNull();
            RuleFor(x => x.ParticipantSid).NotEmpty().NotNull();
            RuleFor(x => x.ParticipantUniversalId).NotEmpty().NotNull();
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