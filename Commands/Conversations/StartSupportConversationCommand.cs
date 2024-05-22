using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.Commands.Conversations
{
    public class StartSupportConversationCommand : IRequest<Conversation>, IValidatabe
    {
        public int PatientId { get; }
        
        public int LocationId { get; }
        
        public int PracticeId { get; }
        
        public string Subject { get; }
        
        public StartSupportConversationCommand(
            int patientId, 
            int locationId, 
            int practiceId,
            string subject)
        {
            PatientId = patientId;
            LocationId = locationId;
            PracticeId = practiceId;
            Subject = subject;
        }

        #region validation

        private class Validator : AbstractValidator<StartSupportConversationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.LocationId).GreaterThan(0);
                RuleFor(x => x.Subject).NotNull().NotEmpty();
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