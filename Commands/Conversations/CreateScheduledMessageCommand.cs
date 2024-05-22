using FluentValidation;
using MediatR;
using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Conversations;
using Microsoft.AspNetCore.Http;

namespace WildHealth.Application.Commands.Conversations
{
    /// <summary>
    /// This command provide creating scheduled message for conversation from employee
    /// </summary>
    public class CreateScheduledMessageCommand:IRequest<ScheduledMessageModel>, IValidatabe
    {
        public int ConversationId { get; }
        public string Message { get; }
        public int ParticipantEmployeeId { get; }
        public DateTime TimeToSend { get; }
        public IFormFile[] Attachments { get; }

        public CreateScheduledMessageCommand(
            int conversationId,
            string message,
            int participantEmployeeId,
            DateTime timeToSend,
            IFormFile[] attachments)
        {
            ConversationId = conversationId;
            Message = message;
            ParticipantEmployeeId = participantEmployeeId;
            TimeToSend = timeToSend;
            Attachments = attachments;
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

        private class Validator : AbstractValidator<CreateScheduledMessageCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Message).NotNull().NotEmpty();
                RuleFor(x => x.ConversationId).GreaterThan(0);
                RuleFor(x => x.ParticipantEmployeeId).GreaterThan(0);
            }
        }

        #endregion
    }
}