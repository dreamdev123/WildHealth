using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    /// <summary>
    /// Command assign support conversation to employee
    /// </summary>
    public class AssignSupportConversationToEmployeeCommand : IRequest<Conversation>, IValidatabe
    {
        public int ConversationId { get; }
        public int EmployeeId { get; }

        public AssignSupportConversationToEmployeeCommand(int employeeId,int conversationId)
        {
            EmployeeId = employeeId;
            ConversationId = conversationId;
        }

        #region validation

        private class Validator : AbstractValidator<AssignSupportConversationToEmployeeCommand>
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
