using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    public class RemoveOrphanedParticipantsCommand : IRequest, IValidatabe
    {
        public string ConversationSid { get; }

        public RemoveOrphanedParticipantsCommand(
            string conversationSid)
        {
            ConversationSid = conversationSid;
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

        private class Validator : AbstractValidator<RemoveOrphanedParticipantsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.ConversationSid).NotNull();
            }
        }

        #endregion
    }
}
