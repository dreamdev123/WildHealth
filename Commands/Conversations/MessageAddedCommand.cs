using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Conversations
{
    public class MessageAddedCommand : IRequest, IValidatabe
    {
        public string AccountSid { get; set; }
        public string Author { get; set; }
        public string Body { get; set; }
        public string ClientIdentity { get; set; }
        public string ConversationSid { get; set; }
        public DateTime DateCreated { get; set; }
        public string EventType { get; set; }
        public int Index { get; set; }
        public string MessageSid { get; set; }
        public string ParticipantSid { get; set; }
        public int RetryCount { get; set; }
        public string Source { get; set; }

        public MessageAddedCommand(
            string accountSid,
            string author,
            string body,
            string clientIdentity,
            string conversationSid,
            DateTime dateCreated,
            string eventType,
            int index,
            string messageSid,
            string participantSid,
            int retryCount,
            string source
        )
        {
            AccountSid = accountSid;
            Author = author;
            Body = body;
            ClientIdentity = clientIdentity;
            ConversationSid = conversationSid;
            DateCreated = dateCreated;
            EventType = eventType;
            Index = index;
            MessageSid = messageSid;
            ParticipantSid = participantSid;
            RetryCount = retryCount;
            Source = source;
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

        private class Validator : AbstractValidator<MessageAddedCommand>
        {
            public Validator()
            {
                RuleFor(x => x.AccountSid).NotNull().NotEmpty();
                RuleFor(x => x.Author).NotNull().NotEmpty();
                RuleFor(x => x.Body).NotNull().NotEmpty();
                RuleFor(x => x.ClientIdentity).NotNull().NotEmpty();
                RuleFor(x => x.ConversationSid).NotNull().NotEmpty();
                RuleFor(x => x.EventType).NotNull().NotEmpty();
                RuleFor(x => x.MessageSid).NotNull().NotEmpty();
                RuleFor(x => x.ParticipantSid).NotNull().NotEmpty();
                RuleFor(x => x.Source).NotNull().NotEmpty();
                RuleFor(x => x.Index).GreaterThan(-1);
                RuleFor(x => x.RetryCount).GreaterThan(-1);
            }
        }

        #endregion
    }
}
