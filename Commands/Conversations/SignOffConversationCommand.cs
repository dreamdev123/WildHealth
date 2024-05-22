using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    public class SignOffConversationCommand : IRequest<Conversation>, IValidatabe
    {
        public int EmployeeId { get; }

        public int ConversationId { get; }

        public SignOffConversationCommand(
            int conversationId,
            int employeeId)
        {
            EmployeeId = employeeId;
            ConversationId = conversationId;
        }

        #region validation

        private class Validator : AbstractValidator<SignOffConversationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.EmployeeId).GreaterThan(0);
                RuleFor(x => x.ConversationId).GreaterThan(0);
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
