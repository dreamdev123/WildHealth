using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    public class UpdateConversationsSettingsCommand : IRequest<ConversationsSettings>, IValidatabe
    {
        public int EmployeeId { get; }

        public bool AwayMessageEnabled { get; }
        
        public DateTime? AwayMessageEnabledFrom { get; }
        
        public DateTime? AwayMessageEnabledTo { get; }
        
        public int? AwayMessageTemplateId { get; }

        public bool MessageForwardingEnabled { get; }

        public int MessageForwardingToEmployeeId { get; }

        public UpdateConversationsSettingsCommand(
            int employeeId,
            bool awayMessageEnabled,
            DateTime? awayMessageEnabledFrom,
            DateTime? awayMessageEnabledTo,
            int? awayMessageTemplateId,
            bool messageForwardingEnabled,
            int messageForwardingToEmployeeId)
        {
            EmployeeId = employeeId;
            AwayMessageEnabled = awayMessageEnabled;
            AwayMessageEnabledFrom = awayMessageEnabledFrom;
            AwayMessageEnabledTo = awayMessageEnabledTo;
            AwayMessageTemplateId = awayMessageTemplateId;
            MessageForwardingEnabled = messageForwardingEnabled;
            MessageForwardingToEmployeeId = messageForwardingToEmployeeId;
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

        private class Validator : AbstractValidator<UpdateConversationsSettingsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.EmployeeId).GreaterThan(0);
                RuleFor(x => x.AwayMessageTemplateId).NotNull().NotEmpty().When(x => x.AwayMessageEnabled);
                RuleFor(x => x.AwayMessageEnabledFrom).NotNull().NotEmpty().When(x => x.AwayMessageEnabled);
                RuleFor(x => x.AwayMessageEnabledTo).NotNull().NotEmpty().When(x => x.AwayMessageEnabled);
                RuleFor(x => x.MessageForwardingToEmployeeId).GreaterThan(0).When(x => x.MessageForwardingEnabled);
            }
        }

        #endregion
    }
}
