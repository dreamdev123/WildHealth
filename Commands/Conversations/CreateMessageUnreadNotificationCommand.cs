using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{

    public class CreateMessageUnreadNotificationCommand: IValidatabe, IRequest<Unit>
    {
        public User User { get; }
        
        public int UnreadMessagesCount { get; }
        
        public int ConversationId { get; }
        
        public NotificationType Type { get; }
        
        public string ConversationType { get;  }

        public CreateMessageUnreadNotificationCommand(
            User user,
            int unreadMessagesCount,
            int conversationId)
        {
            User = user;
            UnreadMessagesCount = unreadMessagesCount;
            Type = NotificationType.UnreadMessagesManual;
            ConversationType = "HealthCare";
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

        private class Validator : AbstractValidator<CreateMessageUnreadNotificationCommand>
        {
            public Validator()
            {
                RuleFor(x=>x.ConversationId).GreaterThan(0);
                RuleFor(x => x.User).NotNull();
                RuleFor(x => x.UnreadMessagesCount).GreaterThan(0);
            }
        }
        #endregion
        
    }
    
        
}

