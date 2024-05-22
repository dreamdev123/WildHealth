using System;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Conversations;

public class GetConversationParticipantIdentityCommand: IRequest<(int, Guid, bool)>
{
    public int ConversationId { get; }
    public string ParticipantSid { get; }

    public GetConversationParticipantIdentityCommand(
        int conversationId,
        string participantSid)
    {
        ConversationId = conversationId;
        ParticipantSid = participantSid;
    }
    

    #region validation

    private class Validator : AbstractValidator<GetConversationParticipantIdentityCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationId).GreaterThan(0);
            RuleFor(x => x.ParticipantSid).NotNull();
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