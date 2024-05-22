using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    public class GetOrCreateConversationsSettingsCommand : IRequest<ConversationsSettings>, IValidatabe
    {
        public int EmployeeId { get; }

        public GetOrCreateConversationsSettingsCommand(
            int employeeId)
        {
            EmployeeId = employeeId;
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

        private class Validator : AbstractValidator<GetOrCreateConversationsSettingsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.EmployeeId).GreaterThan(0);
            }
        }

        #endregion
    }
}
