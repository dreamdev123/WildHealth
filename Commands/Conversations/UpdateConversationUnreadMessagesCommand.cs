using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Users;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    public class UpdateConversationUnreadMessagesCommand : IRequest, IValidatabe
    {
        public User User { get; }
        
        public UpdateConversationUnreadMessagesCommand(User user)
        {
            User = user;
        }

        #region validation

        private class Validator : AbstractValidator<UpdateConversationUnreadMessagesCommand>
        {
            public Validator()
            {
                RuleFor(x => x.User).NotNull().NotEmpty();
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
