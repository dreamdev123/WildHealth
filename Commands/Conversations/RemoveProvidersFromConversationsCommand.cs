using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    public class RemoveProvidersFromConversationsCommand : IRequest, IValidatabe
    {
        public int HealthConversationStaleForDays { get; }
        public int SupportConversationStaleForDays { get; }

        public RemoveProvidersFromConversationsCommand(
            int healthConversationStaleForDays,
            int supportConversationStaleForDays)
        {
            HealthConversationStaleForDays = healthConversationStaleForDays;
            SupportConversationStaleForDays = supportConversationStaleForDays;
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

        private class Validator : AbstractValidator<RemoveProvidersFromConversationsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.HealthConversationStaleForDays).GreaterThan(0);
                RuleFor(x => x.SupportConversationStaleForDays).GreaterThan(0);
            }
        }

        #endregion
    }
}
