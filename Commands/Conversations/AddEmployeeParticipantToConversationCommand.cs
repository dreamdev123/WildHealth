using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    public class AddEmployeeParticipantToConversationCommand : IRequest<Conversation>, IValidatabe
    {
        public int ConversationId { get; }
        public int EmployeeId { get; }
        public int? DelegatedBy { get; }
        public bool? IsActive { get; }

        public AddEmployeeParticipantToConversationCommand(
            int conversationId,
            int employeeId,
            bool? isActive = null,
            int? delegatedBy = null)
        {
            ConversationId = conversationId;
            EmployeeId = employeeId;
            IsActive = isActive;
            DelegatedBy = delegatedBy;
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

        private class Validator : AbstractValidator<AddEmployeeParticipantToConversationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.ConversationId).GreaterThan(0);
                RuleFor(x => x.EmployeeId).GreaterThan(0);
            }
        }

        #endregion
    }
}
