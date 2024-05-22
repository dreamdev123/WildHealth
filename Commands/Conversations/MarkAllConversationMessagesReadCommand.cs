using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    public class MarkAllConversationMessagesReadCommand : IRequest, IValidatabe
    {
        public int ConversationId;

        public MarkAllConversationMessagesReadCommand(
            int conversationId
        )
        {
            ConversationId = conversationId;
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

        private class Validator : AbstractValidator<MarkAllConversationMessagesReadCommand>
        {
            public Validator()
            {
                RuleFor(x => x.ConversationId).NotNull();
            }
        }

        #endregion
    }
}
