using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    public class RecoverParticipantExternalIdCommand : IRequest<ConversationParticipantEmployee?>, IValidatabe
    {
        public string ConversationSid { get; }
        
        public string Identity { get; }

        public RecoverParticipantExternalIdCommand(
            string conversationSid,
            string identity)
        {
            ConversationSid = conversationSid;
            Identity = identity;
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

        private class Validator : AbstractValidator<RecoverParticipantExternalIdCommand>
        {
            public Validator()
            {
                RuleFor(x => x.ConversationSid).NotNull();
                RuleFor(x => x.Identity).NotNull();
            }
        }

        #endregion
    }
}
