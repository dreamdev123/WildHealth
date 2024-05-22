using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    public class UpdateAllConversationParticipantSentIndexesCommand : IRequest, IValidatabe
    {
        public string ConversationVendorExternalId { get; }
        
        public UpdateAllConversationParticipantSentIndexesCommand(
            string conversationVendorExternalId)
        {
            ConversationVendorExternalId = conversationVendorExternalId;
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

        private class Validator : AbstractValidator<UpdateAllConversationParticipantSentIndexesCommand>
        {
            public Validator()
            {
                RuleFor(x => x.ConversationVendorExternalId).NotNull().NotEmpty();
            }
        }

        #endregion
    }
}