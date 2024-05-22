using FluentValidation;
using MediatR;
using System.Collections.Generic;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Twilio.Clients.Models.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    /// <summary>
    /// Command return conversations which are support and open. And employee have same practices for the theme of conversation.
    /// </summary>
    public class GetRecentConversationMessagesCommand : IRequest<ConversationMessageModel[]>, IValidatabe
    {
        public string ConversationSid { get; }
        public int MessageCount { get; }

        public GetRecentConversationMessagesCommand(string conversationSid, int messageCount)
        {
            ConversationSid = conversationSid;
            MessageCount = messageCount;
        }

        #region validation

        private class Validator : AbstractValidator<GetRecentConversationMessagesCommand>
        {
            public Validator()
            {
                RuleFor(x => x.ConversationSid).NotNull();
                RuleFor(x => x.MessageCount).GreaterThan(0);
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
}
