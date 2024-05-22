using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;
using MediatR;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Commands.Conversations
{
    public class SetUnreadMessageUpdateCommand : IRequest<ConversationParticipantMessageReadIndex>, IValidatabe
    {
        public User User { get; }
        public string ParticipantExternalVendorId { get; }
        public string ConversationExternalVendorId { get; }
        public int LastMessageReadIndex { get; }

        public SetUnreadMessageUpdateCommand(
            User user,
            string conversationExternalVendorId,
            string participantExternalVendorId,
            int lastMessageReadIndex
        )
        {
            User = user;
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

        private class Validator : AbstractValidator<SetUnreadMessageUpdateCommand>
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
