using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    public record UpdateMessageSentIndexesForConversationCommand(string ConversationSid) : IRequest, IValidatabe
    {
        #region validation

        private class Validator : AbstractValidator<UpdateMessageSentIndexesForConversationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.ConversationSid).NotNull().NotEmpty();
            }
        }

        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => true;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        #endregion
    }
}