using FluentValidation;
using MediatR;
using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    /// <summary>
    /// Command create new unread notification message
    /// </summary>
    public class CreateConversationMessageUnreadNotificationCommand : IRequest<ConversationMessageUnreadNotification>, IValidatabe
    {
        public DateTime SentAt { get; }
        public int UnreadMessageCount { get; }
        public int LastReadMessageIndex { get; }
        public int UserId { get; }
        public int ConversationId { get; }
        public string ConversationVendorExternalIdentity { get; }
        public string ParticipantVendorExternalIdentity { get; }

        public CreateConversationMessageUnreadNotificationCommand(
            DateTime sentAt,
            int unreadMessageCount,
            int lastReadMessageIndex,
            int userId,
            int conversationId,
            string conversationVendorExternalIdentity,
            string participantVendorExternalIdentity
            )
        {
            SentAt = sentAt;
            UnreadMessageCount = unreadMessageCount;
            LastReadMessageIndex = lastReadMessageIndex;
            UserId = userId;
            ConversationId = conversationId;
            ConversationVendorExternalIdentity = conversationVendorExternalIdentity;
            ParticipantVendorExternalIdentity = participantVendorExternalIdentity;
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

        private class Validator : AbstractValidator<CreateConversationMessageUnreadNotificationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.UserId).GreaterThan(0);
                RuleFor(x => x.ConversationId).GreaterThan(0);
                RuleFor(x => x.UnreadMessageCount).GreaterThan(-1);
                RuleFor(x => x.LastReadMessageIndex).NotNull();
                RuleFor(x => x.ConversationVendorExternalIdentity).NotNull().NotEmpty();
                RuleFor(x => x.ParticipantVendorExternalIdentity).NotNull().NotEmpty();
                RuleFor(x => x.SentAt).NotNull();
            }
        }

        #endregion
    }
}
