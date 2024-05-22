using FluentValidation;
using WildHealth.Domain.Entities.Users;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    public class CreateConversationAuthTokenCommand : IRequest<User>, IValidatabe
    {
        public int UserId { get; }

        public CreateConversationAuthTokenCommand(
            int userId)
        {
            UserId = userId;
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

        private class Validator : AbstractValidator<CreateConversationAuthTokenCommand>
        {
            public Validator()
            {
                RuleFor(x => x.UserId).GreaterThan(0);
            }
        }

        #endregion
    }
}