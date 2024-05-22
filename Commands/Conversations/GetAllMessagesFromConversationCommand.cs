using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Twilio.Clients.Models.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    public class GetAllMessagesFromConversationCommand : IRequest<ConversationMessagesModel>
    {
        public string VendorExternalId { get; }

        public GetAllMessagesFromConversationCommand(
           string vendorExternalId)
        {
            VendorExternalId = vendorExternalId;
        }

        #region validation

        private class Validator : AbstractValidator<GetAllMessagesFromConversationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.VendorExternalId).NotEmpty();
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
