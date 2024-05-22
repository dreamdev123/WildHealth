using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    public class LastReadMessageUpdateCommand : IRequest<ConversationParticipantMessageReadIndex>, IValidatabe
    {
        public int ConversationId { get; }
        public string ParticipantExternalVendorId { get; }
        public string ConversationExternalVendorId { get; }
        public int LastMessageReadIndex { get; }

        public LastReadMessageUpdateCommand(
            int conversationId,
            string conversationExternalVendorId,
            string participantExternalVendorId,
            int lastMessageReadIndex
        )
        {
            ConversationId = conversationId;
            ParticipantExternalVendorId = participantExternalVendorId;
            ConversationExternalVendorId = conversationExternalVendorId;
            LastMessageReadIndex = lastMessageReadIndex;
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

        private class Validator : AbstractValidator<LastReadMessageUpdateCommand>
        {
            public Validator()
            {
                RuleFor(x => x.LastMessageReadIndex).GreaterThan(-1);
                RuleFor(x => x.ConversationExternalVendorId).NotNull();
                RuleFor(x => x.ParticipantExternalVendorId).NotNull();
            }
        }

        #endregion
    }
}
