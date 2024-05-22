using System;
using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    /// <summary>
    /// This command send notification on platform if new message was sent and user now not in a conversation
    /// </summary>
    public class ProcessNewMessageNotificationFromTwilioCommand : IRequest, IValidatabe
    {
        public string ConversationSid { get; }
        public string ParticipantSid { get; }
        public int Index { get; }
        public string[] FileNames { get; }
        public string Message { get; set; }

        public ProcessNewMessageNotificationFromTwilioCommand(
            string conversationSid,
            string participantSid,
            int index,
            string[] fileNames,
            string message)
        {
            ConversationSid = conversationSid;
            ParticipantSid = participantSid;
            Index = index;
            FileNames = fileNames;
            Message = message;
        }

        #region validation

        private class Validator : AbstractValidator<ProcessNewMessageNotificationFromTwilioCommand>
        {
            public Validator()
            {
                RuleFor(x => x.ConversationSid).NotEmpty();
                RuleFor(x => x.ParticipantSid).NotEmpty();
                RuleFor(x => x.Index).GreaterThanOrEqualTo(0);
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
