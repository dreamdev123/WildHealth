using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    public class RemoveEmployeeParticipantFromConversationCommand : IRequest<Conversation>, IValidatabe
    {
        public int ConversationId { get; }
        public int UserId { get; }

        public RemoveEmployeeParticipantFromConversationCommand(
            int conversationId,
            int userId)
        {
            ConversationId = conversationId;
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

        private class Validator : AbstractValidator<RemoveEmployeeParticipantFromConversationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.ConversationId).GreaterThan(0);
                RuleFor(x => x.UserId).GreaterThan(0);
            }
        }

        #endregion
    }
}
