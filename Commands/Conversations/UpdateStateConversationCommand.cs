using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    /// <summary>
    /// Command update conversation status
    /// </summary>
    public class UpdateStateConversationCommand : IRequest<Conversation>, IValidatabe
    {
        public int ConversationId { get; set; }
        public ConversationState ConversationState { get; set; }
        
        public int StateChangeEmployeeId { get; set; }

        public UpdateStateConversationCommand()
        {
        }

        public UpdateStateConversationCommand(
            int conversationId,
            ConversationState conversationState,
            int employeeId)
        {
            ConversationId = conversationId;
            ConversationState = conversationState;
            StateChangeEmployeeId = employeeId;
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

        private class Validator : AbstractValidator<UpdateStateConversationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.ConversationId).GreaterThan(0);
                RuleFor(x => x.ConversationState).IsInEnum();
            }
        }

        #endregion
    }
}
