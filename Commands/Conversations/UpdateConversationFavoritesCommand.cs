using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    /// <summary>
    /// Command update conversation status
    /// </summary>
    public class UpdateConversationFavoritesCommand : IRequest<Conversation>, IValidatabe
    {
        public int ConversationId { get; }
        public int EmployeeId { get; }
        public bool IsAdd { get; }

        public UpdateConversationFavoritesCommand(
            int conversationId,
            int employeeId,
            bool isAdd)
        {
            ConversationId = conversationId;
            EmployeeId = employeeId;
            IsAdd = isAdd;
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

        private class Validator : AbstractValidator<UpdateConversationFavoritesCommand>
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
